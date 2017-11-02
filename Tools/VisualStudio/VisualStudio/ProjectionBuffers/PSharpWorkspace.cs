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
using EnvDTE;

namespace Microsoft.PSharp.VisualStudio
{
    class PSharpWorkspace : Workspace
    {
        VisualStudioWorkspace vsWorkspace;
        ITextBufferFactoryService textBufferFactory;
        IProjectionBufferFactoryService projectionBufferFactory;
        IContentType csharpContentType;
        IContentType projectionContentType;
        IContentType inertContentType;

        ProjectionBufferGraph projectionBufferGraph;
        private ProjectionInfos projectionInfos;

        private Dictionary<ITextBuffer, ProjectionBufferGraph> csProjectionBufferMap = new Dictionary<ITextBuffer, ProjectionBufferGraph>();
        CodeAnalysis.Project projectionProject;
        string projectionProjectName = "P#ProjectionProject_" + Guid.NewGuid().ToString();

        internal static SpanTrackingMode SpanTrackingMode = SpanTrackingMode.EdgeExclusive; // TODO is this the right option

        internal PSharpWorkspace(VisualStudioWorkspace vsWorkspace, ITextBufferFactoryService textBufferFactory,
                                 IProjectionBufferFactoryService projectionBufferFactory, IContentTypeRegistryService contentTypeRegistry)
            : base(vsWorkspace.Services.HostServices, "P# Workspace")
        {
            this.vsWorkspace = vsWorkspace;
            this.textBufferFactory = textBufferFactory;
            this.projectionBufferFactory = projectionBufferFactory;
            this.csharpContentType = contentTypeRegistry.GetContentType("CSharp");
            this.projectionContentType = contentTypeRegistry.GetContentType("Projection");
            this.inertContentType = contentTypeRegistry.GetContentType("inert");
#if false // TODO: vsWorkspace.WorkspaceChanged processing
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
            configuration.ForVsLanguageService = true;

            var context = CompilationContext.Create(configuration).LoadSolution(text);

            try
            {
                ParsingEngine.Create(context).Run();
                RewritingEngine.Create(context).Run();

                var pSharpProgram = context.GetProjects()[0].PSharpPrograms[0];
                this.projectionInfos = pSharpProgram.ProjectionInfos;

                var csharpText = pSharpProgram.GetSyntaxTree().ToString();
            }
            catch (ParsingException ex)
            {
                // TODO: ParsingException
            }
            catch (RewritingException ex)
            {
                // TODO: RewritingException
            }
            catch (Exception)
            {
                // TODO: other Exception
            }
        }

        private (IProjectionBuffer, List<ITrackingSpan>) CreateCSharpProjectionBuffer(ITextBuffer vsTextBuffer)
        {
            var csProjTrackingSpans = CreateCSharpTrackingSpans(vsTextBuffer);
            var csProjectionBuffer = projectionBufferFactory.CreateProjectionBuffer(
                null, // TODO projectionEditResolver
                csProjTrackingSpans.Select(ts => (object)ts).ToList(),
                ProjectionBufferOptions.None,
                csharpContentType
            );
            csProjectionBuffer.Changed += CsProjectionBuffer_Changed;
            csProjectionBuffer.SourceSpansChanged += CsProjectionBuffer_SourceSpansChanged;
            return (csProjectionBuffer, csProjTrackingSpans);
        }

        private void CsProjectionBuffer_Changed(object sender, TextContentChangedEventArgs args)
        {
            return; // TODO editing support
        }

        private void CsProjectionBuffer_SourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs args)
        {
            return; // TODO editing support
        }

        private (IProjectionBuffer, (List<ITrackingSpan>, ISet<ITrackingSpan>)) CreatePSharpViewProjectionBuffer(IProjectionBuffer csProjectionBuffer, ITextBuffer vsTextBuffer)
        {
            var (psViewProjTrackingSpans, embeddedPSharpTrackingSpans) = CreatePSharpViewTrackingSpans(csProjectionBuffer, vsTextBuffer);
            var psViewProjectionBuffer = projectionBufferFactory.CreateProjectionBuffer(
                null, // TODO projectionEditResolver
                psViewProjTrackingSpans.Select(ts => (object)ts).ToList(),
                ProjectionBufferOptions.None,
                projectionContentType
            );
            psViewProjectionBuffer.Changed += PsViewProjectionBuffer_Changed;
            psViewProjectionBuffer.SourceSpansChanged += PsViewProjectionBuffer_SourceSpansChanged;
            vsTextBuffer.Changed += VsTextBuffer_Changed;
            return (psViewProjectionBuffer, (psViewProjTrackingSpans, embeddedPSharpTrackingSpans));
        }

        private void VsTextBuffer_Changed(object sender, TextContentChangedEventArgs args)
        {
            var isEmbeddedCSharp = this.projectionBufferGraph.IsEmbeddedPSharpPoint(args.Changes.First().NewPosition);
            return; // TODO editing support
        }

        private void PsViewProjectionBuffer_Changed(object sender, TextContentChangedEventArgs args)
        {
            return; // TODO editing support
        }

        private void PsViewProjectionBuffer_SourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs e)
        {
            return; // TODO editing support
        }

        private List<ITrackingSpan> CreateCSharpTrackingSpans(ITextBuffer vsTextBuffer)
        {
            // The C# ProjectionBuffer has two sources:
            // 1c#. The PSharp base textBuffer, for all segments of C# code chunks that are a copy of unchanged PSharp text. These
            //      chunks are moved from their positions in the original P# file due to separating the rewritten header from the code.
            //      Each chunk contains a mix of "copied from P#" code broken up by zero or more "rewritten from P# to C#" segments
            //      (e.g. "machine" -> "MachineId").
            // 2c#. The "inert" text buffer created from the full rewritten C# text. This buffer supplies the segments of C# code for
            //      the Roslyn C# buffer handlers, but Roslyn will not propose tags etc. for these segments. These segments are:
            //      a.  Any code rewritten from P# to C# text (e.g. rewritten headers such as "machine M1" -> "class M1: Machine").
            //          This includes both RewrittenCodeTerms and RewrittenHeaderStrings.
            //      b.  The P# code that is between C# code chunks, which is essentially the entire P# file outside the C# code
            //          chunks. This text is copied from corresponding spans of the rewritten C# file.
            var csSpans = new List<(int Start, int End, ITrackingSpan TrackingSpan)>();
            var inertTextBuffer = textBufferFactory.CreateTextBuffer(this.projectionInfos.RewrittenCSharpText, inertContentType);
            var inertBufferOffset = 0;

            void AddOrCoalesceInertSpan(int csEnd)
            {
                if (csSpans.Count > 0)
                {
                    var lastSpan = csSpans[csSpans.Count - 1];
                    if (lastSpan.TrackingSpan == null && lastSpan.End == inertBufferOffset)
                    {
                        csSpans[csSpans.Count - 1] = (lastSpan.Start, csEnd, null);
                        inertBufferOffset = csEnd;
                        return;
                    }
                }
                csSpans.Add((inertBufferOffset, csEnd, null));
                inertBufferOffset = csEnd;
            }

            void projectFromPSharpSpan(int psStart, int psEnd)
            {
                if (psEnd > psStart)
                {
                    // The P# view projection buffer gets its P# data from the VS editing text buffer.
                    var span = new Span(psStart, psEnd - psStart);
                    //var unused = vsTextBuffer.CurrentSnapshot.AsText().ToString().Substring(span.Start, span.Length);
                    csSpans.Add((-1, -1, vsTextBuffer.CurrentSnapshot.CreateTrackingSpan(span, SpanTrackingMode)));
                }
            }

            ITrackingSpan createTrackingSpan((int start, int end, ITrackingSpan trackingSpan) span)
            {
                return (span.trackingSpan != null)
                    ? span.trackingSpan
                    : inertTextBuffer.CurrentSnapshot.CreateTrackingSpan(span.start, span.end - span.start, SpanTrackingMode);
            }

            foreach (var projInfo in this.projectionInfos.OrderedProjectionInfos)
            {
                if (projInfo.HasRewrittenHeader)
                {
                    AddOrCoalesceInertSpan(projInfo.Header.RewrittenEnd);
                }

                if (projInfo.HasCode)
                {
                    AddOrCoalesceInertSpan(projInfo.CodeChunk.RewrittenStart);
                    var psBufferOffset = projInfo.CodeChunk.OriginalStart;
                    foreach (var term in projInfo.RewrittenCodeTerms)
                    {
                        projectFromPSharpSpan(psBufferOffset, term.OriginalStart);
                        inertBufferOffset = term.RewrittenStart;
                        AddOrCoalesceInertSpan(term.RewrittenEnd);
                        psBufferOffset = term.OriginalEnd;
                    }

                    // Add any leftovers in the code chunk.
                    projectFromPSharpSpan(psBufferOffset, projInfo.CodeChunk.OriginalEnd);
                    inertBufferOffset = projInfo.CodeChunk.RewrittenStart + projInfo.CodeChunk.RewrittenLength;
                }
            }

            // Add any leftovers from the C# text
            AddOrCoalesceInertSpan(inertTextBuffer.CurrentSnapshot.Length);
            return csSpans.Select(span => createTrackingSpan(span)).ToList();
        }

        private (List<ITrackingSpan>, ISet<ITrackingSpan>) CreatePSharpViewTrackingSpans(IProjectionBuffer csProjectionBuffer, ITextBuffer vsTextBuffer)
        {
            // The P# View ProjectionBuffer is what is seen by Visual Studio. It has two sources:
            // 1p#. The CSharp projectionBuffer, tracking the same unaltered C# segments of code chunks as in 
            //      CreateCSharpTrackingSpans:1c#. This allows the C# buffer processing to be used.
            // 2p#. vsTextBuffer (passed in) for all other unmodified P# code. This includes:
            //      a.  The original P# text for the segments of C# code chunks that were rewritten from P# to C#
            //          (e.g. for the rewritten "machine" -> "MachineId", this would contain "machine").
            //      b.  The P# code that is between C# code chunks, which is essentially the entire P# file outside the C# code
            //          chunks, exactly as in CreateCSharpTrackingSpans:2c#.b.
            // Additional notes:
            // 1. It is important that the inert buffer spans in the C# ProjectionBuffer and the P# spans in the P# View
            //    ProjectionBuffer line up correctly (even though they come from different "files", the original P# file and the
            //    rewritten C# in-memory "file"). The ProjectionInfos are used to track offsets into these two buffers.
            //    Similarly, the unchanged spans of C# code must be projected from their position in the C# ProjectionBuffer
            //    to their positions in the P# View ProjectionBuffer.
            // 2. The sections of the C# ProjectionBuffer that contain P#-mapped code cannot overlap with the sections of the
            //    P# View ProjectionBuffer that contain P#-mapped code. The ProjectionBuffer system will not allow buffer
            //    overlaps, even when the buffers are buried one or more layers below the surface.
            var psViewSpans = new List<(int Start, int End, bool IsPSharp, bool IsEmbeddedPSharp)>();
            var psBufferOffset = 0;

            void AddOrCoalesceSpan(int start, int end, bool isPSharp, bool isEmbeddedPSharp)
            {
                if (psViewSpans.Count > 0)
                {
                    var lastSpan = psViewSpans[psViewSpans.Count - 1];
                    if (lastSpan.IsPSharp == isPSharp && !lastSpan.IsEmbeddedPSharp && !isEmbeddedPSharp && lastSpan.End == start)
                    {
                        psViewSpans[psViewSpans.Count - 1] = (lastSpan.Start, end, isPSharp, false);
                        return;
                    }
                }
                psViewSpans.Add((start, end, isPSharp, isEmbeddedPSharp));
            }

            void projectFromCSharpSpan(int csStart, int csEnd)
            {
                if (csEnd > csStart)
                {
                    // The P# view projection buffer gets this span from the C# projection buffer (hence a 'graph').
                    //var unused = csSnapshot.AsText().ToString().Substring(csStart, csEnd - csStart);
                    AddOrCoalesceSpan(csStart, csEnd, isPSharp:false, isEmbeddedPSharp:false);
                }
            }

            void projectFromPSharpSpan(int psEnd, bool isEmbeddedPSharp = false)
            {
                if (psEnd > psBufferOffset)
                {
                    // The P# view projection buffer copies the original P# text to a new P# text buffer which is then a
                    // source to the ProjectionBuffer.
                    //var unused = vsTextBuffer.CurrentSnapshot.AsText().ToString().Substring(psBufferOffset, psEnd - psBufferOffset);
                    AddOrCoalesceSpan(psBufferOffset, psEnd, isPSharp:true, isEmbeddedPSharp:isEmbeddedPSharp);
                    psBufferOffset = psEnd;
                }
            }

            var embeddedPSharpTrackingSpans = new HashSet<ITrackingSpan>();
            ITrackingSpan createTrackingSpan((int start, int end, bool isPSharp, bool isEmbeddedPSharp) span)
            {
                var length = span.end - span.start;
                var buffer = span.isPSharp ? vsTextBuffer : csProjectionBuffer;
                var trackingSpan = buffer.CurrentSnapshot.CreateTrackingSpan(span.start, length, SpanTrackingMode);
                if (span.isEmbeddedPSharp)
                    embeddedPSharpTrackingSpans.Add(trackingSpan);
                return trackingSpan;
            }

            foreach (var projInfo in this.projectionInfos.OrderedProjectionInfos)
            {
                // All P#-to-C# text has already been placed into the C# projection buffer as inert text.
                // We will map the original P# text for the same spans to the P# view projection buffer as P# text.
                // Also, code chunks that were moved during P#-to-C# rewriting will be redirected to the P# projection
                // buffer at their original positions.
                if (projInfo.HasRewrittenHeader)
                {
                    // If it has a code chunk, copy until the start of that, else stop at the end of the rewritten header.
                    var end = projInfo.HasRewrittenCodeTerms
                        ? projInfo.CodeChunk.OriginalStart
                        : projInfo.Header.OriginalStart + projInfo.Header.OriginalLength;
                    projectFromPSharpSpan(end);
                }

                if (projInfo.HasCode)
                {
                    projectFromPSharpSpan(projInfo.CodeChunk.OriginalStart);
                    var csBufferOffset = projInfo.CodeChunk.RewrittenStart;
                    foreach (var term in projInfo.RewrittenCodeTerms)
                    {
                        projectFromCSharpSpan(csBufferOffset, term.RewrittenStart);
                        csBufferOffset = term.RewrittenEnd;
                        psBufferOffset = term.OriginalStart;
                        projectFromPSharpSpan(term.OriginalEnd, isEmbeddedPSharp:true);
                    }

                    // Add any leftovers in the code chunk.
                    projectFromCSharpSpan(csBufferOffset, projInfo.CodeChunk.RewrittenEnd);
                    psBufferOffset = projInfo.CodeChunk.OriginalEnd;
                }
            }

            // Add any leftovers from the base P# buffer
            projectFromPSharpSpan(vsTextBuffer.CurrentSnapshot.Length);
            return (psViewSpans.Select(span => createTrackingSpan(span)).ToList(), embeddedPSharpTrackingSpans);
        }

        private ProjectionBufferGraph CreateProjectionBufferGraph(ITextBuffer vsTextBuffer)
        {
            var (csProjectionBuffer, csProjTrackingSpans) = CreateCSharpProjectionBuffer(vsTextBuffer);
            var (psViewProjectionBuffer, (psViewProjTrackingSpans, embeddedPsTrackingSpans)) = CreatePSharpViewProjectionBuffer(csProjectionBuffer, vsTextBuffer);

            this.projectionBufferGraph = new ProjectionBufferGraph
            {
                PSharpDiskBuffer = vsTextBuffer,
                CSharpProjectionBuffer = csProjectionBuffer,
                PSharpViewProjectionBuffer = psViewProjectionBuffer,
                CSharpProjTrackingSpans = csProjTrackingSpans.ToArray(),
                PSharpViewProjTrackingSpans = psViewProjTrackingSpans.ToArray(),
                EmbeddedPSharpTrackingSpans = embeddedPsTrackingSpans
            };
            psViewProjectionBuffer.Properties.AddProperty(typeof(ProjectionBufferGraph), projectionBufferGraph);
            return this.projectionBufferGraph;
        }

        private void AddProjectedDocuments(ProjectionBufferGraph graph)
        {
            EnsureProject();

            // Only the CSharpProjectionBuffer gets an added document.
            graph.CSharpProjectionDocument = AddProjectedDocument(graph.CSharpProjectionBuffer, "C#Projection");
        }

        private CodeAnalysis.Document AddProjectedDocument(IProjectionBuffer projectionBuffer, string documentNamePrefix)
        {
            var documentInfo = CreateProjectedDocumentInfo(projectionBuffer, documentNamePrefix);
            var csTextContainer = projectionBuffer.AsTextContainer();
            var mutatedSolution = projectionProject.Solution.AddDocument(documentInfo);

            // Update to the mutated project and solution.
            this.projectionProject = mutatedSolution.GetProject(this.projectionProject.Id);
            base.SetCurrentSolution(mutatedSolution);

            // base.OnDocumentAdded(documentInfo); // TODO needed? throws "'<doc id>' is already part of the workspace
            base.OnDocumentOpened(documentInfo.Id, csTextContainer);

            return base.CurrentSolution.GetDocument(documentInfo.Id);
        }

        private DocumentInfo CreateProjectedDocumentInfo(IProjectionBuffer projectionBuffer, string documentNamePrefix)
        {
            var csTextContainer = projectionBuffer.AsTextContainer();
            var loader = TextLoader.From(TextAndVersion.Create(csTextContainer.CurrentText, VersionStamp.Create()));
            var documentName = $"{documentNamePrefix}_{Guid.NewGuid().ToString()}.cs";
            return DocumentInfo.Create(DocumentId.CreateNewId(projectionProject.Id), documentName, loader: loader);
        }

        public void EnsureProject()
        {
            if (this.projectionProject == null)
            {
                var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(_DTE)) as EnvDTE.DTE;
                // TODO workaround only--FIXME: Sometimes we're constructed/invoked before ActiveDocument or CurrentSolution.Projects have been set
                for (var iter = 0; iter < 25; System.Threading.Thread.Sleep(200), ++iter)
                {
                    var activeDoc = dte?.ActiveDocument;
                    var activeProject = activeDoc?.ProjectItem?.ContainingProject;
                    if (activeProject != null)
                    {
                        var documentId = this.vsWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(activeDoc.FullName).FirstOrDefault();
                        if (documentId != null)
                        {
                            this.projectionProject = this.vsWorkspace.CurrentSolution.GetDocument(documentId).Project;
                        }
                        if (this.projectionProject == null)
                        {
                            this.projectionProject = this.vsWorkspace.CurrentSolution.Projects.FirstOrDefault(proj => proj.FilePath == activeProject.FullName);
                        }
                    }
                    if (this.projectionProject != null)
                    {
                        return;
                    }
                }

                // TODO We should not get here.
                var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), projectionProjectName, projectionProjectName, LanguageNames.CSharp);
                var mutatedSolution = vsWorkspace.CurrentSolution.AddProject(projectInfo);
                this.OnProjectAdded(projectInfo);   // TODO needed?
                this.UpdateReferencesAfterAdd();    // TODO needed?
                this.projectionProject = mutatedSolution.GetProject(projectInfo.Id);
            }
        }
    }
}
