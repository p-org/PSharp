using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using System;
using Microsoft.VisualStudio.LanguageServices;
using System.Linq;
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.VisualStudio
{
    class PSharpWorkspace : Workspace
    {
        VisualStudioWorkspace vsWorkspace;
        ITextBufferFactoryService textBufferFactory;
        IProjectionBufferFactoryService projectionBufferFactory;
        IContentType csharpContentType;
        IContentType psharpContentType;
        IContentType projectionContentType;

        private RewrittenTerms pSharpRewrittenTerms;
        private ITextBuffer pSharpToCSharpRewrittenTextBuffer;

        private Dictionary<ITextBuffer, ProjectionBufferGraph> csProjectionBufferMap = new Dictionary<ITextBuffer, ProjectionBufferGraph>();
        Project projectionProject;
        string projectionProjectName = "P#ProjectionProject_" + Guid.NewGuid().ToString();

        internal PSharpWorkspace(VisualStudioWorkspace vsWorkspace, ITextBufferFactoryService textBufferFactory,
                                 IProjectionBufferFactoryService projectionBufferFactory, IContentTypeRegistryService contentTypeRegistry)
            : base(vsWorkspace.Services.HostServices, "P# Workspace")
        {
            this.vsWorkspace = vsWorkspace;
            this.textBufferFactory = textBufferFactory;
            this.projectionBufferFactory = projectionBufferFactory;
            this.csharpContentType = contentTypeRegistry.GetContentType("CSharp");
            this.psharpContentType = contentTypeRegistry.GetContentType("PSharp");
            this.projectionContentType = contentTypeRegistry.GetContentType("Projection");
#if false // TODO 
            vsWorkspace.WorkspaceChanged += VisualStudioWorkspace_WorkspaceChanged;
        }

        private void VisualStudioWorkspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            //throw new NotImplementedException();
#endif
        }

        internal ProjectionBufferGraph InsertProjectionBufferGraph(ITextBuffer vsTextBuffer)
        {
            ProjectionBufferGraph graph;
            if (!this.csProjectionBufferMap.TryGetValue(vsTextBuffer, out graph))
            {
                this.GetPSharpRewrites(vsTextBuffer.CurrentSnapshot.GetText());
                graph = this.CreateProjectionBufferGraph(vsTextBuffer);
                this.AddProjectedDocuments(graph);
                this.csProjectionBufferMap[vsTextBuffer] = graph;
            }
            return graph;
        }

        private void GetPSharpRewrites(string text)
        {
            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var context = CompilationContext.Create(configuration).LoadSolution(text);

            try
            {
                ParsingEngine.Create(context).Run();
                RewritingEngine.Create(context).Run();

                var pSharpProgram = context.GetProjects()[0].PSharpPrograms[0];
                this.pSharpRewrittenTerms = pSharpProgram.RewrittenTerms;

                var csharpText = pSharpProgram.GetSyntaxTree().ToString();
                this.pSharpToCSharpRewrittenTextBuffer = this.textBufferFactory.CreateTextBuffer(csharpText, this.csharpContentType);
            }
            catch (ParsingException ex)
            {
                // TODO
            }
            catch (RewritingException ex)
            {
                // TODO
            }
            catch (Exception)
            {
                // TODO
            }
        }

        private ProjectionBufferGraph CreateProjectionBufferGraph(ITextBuffer vsTextBuffer)
        {
            var rewrittenSpans = RewriteTextBuffers.GetRewrittenSpans(this.pSharpRewrittenTerms);
            var (csProjTrackingSpans, psViewProjPartialSpans) = CreateTrackingSpans(rewrittenSpans);

            var csProjectionBuffer = projectionBufferFactory.CreateProjectionBuffer(
                null, // TODO projectionEditResolver
                csProjTrackingSpans.Select(ts => (object)ts).ToList(),
                ProjectionBufferOptions.None,
                csharpContentType
            );
            var csSnapshot = csProjectionBuffer.CurrentSnapshot;

            var psViewProjTrackingSpans = psViewProjPartialSpans.Select(sp => sp.Item2 ?? csSnapshot.CreateTrackingSpan(sp.Item1, RewriteTextBuffers.SpanTrackingMode));
            var psViewProjectionBuffer = projectionBufferFactory.CreateProjectionBuffer(
                null, // TODO projectionEditResolver
                psViewProjTrackingSpans.Select(ts => (object)ts).ToList(),
                ProjectionBufferOptions.None,
                projectionContentType
            );

            return new ProjectionBufferGraph
            {
                PSharpDiskBuffer = vsTextBuffer,
                //TODO  StaticRewriteBuffer = StaticRewriter.StaticRewriteTextBuffer,
                CSharpProjectionBuffer = csProjectionBuffer,
                PSharpViewProjectionBuffer = psViewProjectionBuffer,
                CSharpProjTrackingSpans = csProjTrackingSpans.ToArray(),
                PSharpViewProjTrackingSpans = psViewProjTrackingSpans.ToArray()
            };
        }

        private (List<ITrackingSpan>, List<Tuple<Span, ITrackingSpan>>) CreateTrackingSpans(RewrittenSpan[] rewrittenSpans)
        {
            var baseTextBuffer = this.pSharpToCSharpRewrittenTextBuffer;
            var baseSnapshot = baseTextBuffer.CurrentSnapshot;

            // The C# ProjectionBuffer has two sources:
            // 1c#. The CSharp base textBuffer (containing a copy of unchanged but moved text from the original P# file)
            // 2c#. The rewritten P# to C# text (copied to an 'inert' textBuffer)
            var csProjTrackingSpans = new List<ITrackingSpan>();

            // The P# ProjectionBuffer has two sources:
            // 1p#. The CSharp projectionBuffer (when created in return to caller); tracking the same text span as in 1c#.
            //      The fact that we can't fill in the tracking span yet is why the name is 'Partial'.
            // 2p#. The original (pre-rewritten) P# text (copied to a 'psharp' textbuffer).
            var psViewProjPartialSpans = new List<Tuple<Span, ITrackingSpan>>();

            // A placeholder for below because we can't create the tracking span yet.
            const ITrackingSpan PendingTrackingSpan = null;

            var baseTextBufferOffset = 0;
            var csTargetOffset = 0;         // offset into the generated C# projectionBuffer
            foreach (var rewrittenSpan in rewrittenSpans)
            {
                var length = rewrittenSpan.RewrittenTerm.Start - baseTextBufferOffset;
                if (length > 0)
                {
                    // The C# projection buffer gets this span from the base C# textBuffer; the P# view projection buffer gets
                    // it from the C# projection buffer (hence a 'graph').
                    csProjTrackingSpans.Add(baseSnapshot.CreateTrackingSpan(baseTextBufferOffset, length, RewriteTextBuffers.SpanTrackingMode));
                    psViewProjPartialSpans.Add(Tuple.Create(new Span(csTargetOffset, length), PendingTrackingSpan));
                }

                // The C# projection buffer gets the rewritten P# to C#; the P# view projection buffer gets the original P#.
                csProjTrackingSpans.Add(rewrittenSpan.ReplacementTrackingSpan);
                psViewProjPartialSpans.Add(Tuple.Create(rewrittenSpan.OriginalSpan, rewrittenSpan.OriginalTrackingSpan));

                // Update offsets
                baseTextBufferOffset += length + rewrittenSpan.ReplacementSpan.Length;
                csTargetOffset += length + rewrittenSpan.ReplacementSpan.Length;
            }

            // Add any leftovers in the base C# buffer
            if (baseTextBufferOffset < baseSnapshot.Length - 1)
            {
                // As above, the C# projection buffer gets this span from the base C# textBuffer; the P# view projection buffer gets
                // it from the C# projection buffer (hence a 'graph').
                var length = baseSnapshot.Length - baseTextBufferOffset;
                csProjTrackingSpans.Add(baseSnapshot.CreateTrackingSpan(baseTextBufferOffset, length, RewriteTextBuffers.SpanTrackingMode));
                psViewProjPartialSpans.Add(Tuple.Create(new Span(csTargetOffset, length), PendingTrackingSpan));
            }

            return (csProjTrackingSpans, psViewProjPartialSpans);
        }

        private void AddProjectedDocuments(ProjectionBufferGraph graph)
        {
            EnsureProject();

            // Only the CSharpProjectionBuffer gets an added document.
            graph.CSharpProjectionDocument = AddProjectedDocument(graph.CSharpProjectionBuffer, "C#Projection");
            var mutatedSolution = this.projectionProject.Solution;
            base.SetCurrentSolution(mutatedSolution);
        }

        private Document AddProjectedDocument(IProjectionBuffer projectionBuffer, string documentNamePrefix)
        {
            var documentInfo = CreateProjectedDocumentInfo(projectionBuffer, documentNamePrefix);
            var csTextContainer = projectionBuffer.AsTextContainer();
            var mutatedSolution = projectionProject.Solution.AddDocument(documentInfo);
            base.OnDocumentAdded(documentInfo); // TODO needed?
            base.OnDocumentOpened(documentInfo.Id, csTextContainer);

            // Update to the mutated project.
            this.projectionProject = mutatedSolution.GetProject(this.projectionProject.Id);
            return base.CurrentSolution.GetDocument(documentInfo.Id);
        }

        private DocumentInfo CreateProjectedDocumentInfo(IProjectionBuffer projectionBuffer, string documentNamePrefix)
        {
            var csTextContainer = projectionBuffer.AsTextContainer();
            var loader = TextLoader.From(TextAndVersion.Create(csTextContainer.CurrentText, VersionStamp.Create()));
            var documentName = $"{documentNamePrefix}_{Guid.NewGuid().ToString()}";
            return DocumentInfo.Create(DocumentId.CreateNewId(projectionProject.Id), documentName, loader: loader);
        }

        public void EnsureProject()
        {
            if (this.projectionProject == null)
            {
                var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), projectionProjectName, projectionProjectName, LanguageNames.CSharp);
                var mutatedSolution = vsWorkspace.CurrentSolution.AddProject(projectInfo);
                this.OnProjectAdded(projectInfo);   // TODO needed?
                this.UpdateReferencesAfterAdd();    // TODO needed?
                this.projectionProject = mutatedSolution.GetProject(projectInfo.Id);
            }
        }
    }
}
