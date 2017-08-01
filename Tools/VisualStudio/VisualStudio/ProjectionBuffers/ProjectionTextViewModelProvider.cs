using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// This provider is called to create a custom text view model (based on a projection buffer) for any PSharp WpfTextView.
    /// </summary>
    [Export(typeof(ITextViewModelProvider)), ContentType("PSharp"), TextViewRole(PredefinedTextViewRoles.Document)]
    internal class ProjectionTextViewModelProvider : ITextViewModelProvider
    {
        [Import]
        private VisualStudioWorkspace vsWorkspace = null;

        [Import]
        private IProjectionBufferFactoryService projectionBufferFactory = null;

        [Import]
        private IContentTypeRegistryService contentTypeRegistry = null;

        [Import]
        private ITextBufferFactoryService textBufferFactory = null;

        private Dictionary<ITextBuffer, ProjectionBufferGraph> bufferToGraphMap = new Dictionary<ITextBuffer, ProjectionBufferGraph>();

        public ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles)
        {
            ProjectionBufferGraph graph;
            if (!bufferToGraphMap.TryGetValue(dataModel.DataBuffer, out graph))
            {
                RewriteTextBuffers.Initialize(textBufferFactory, contentTypeRegistry.GetContentType("psharp"),
                                              contentTypeRegistry.GetContentType("inert"));
                var psharpWorkspace = new PSharpWorkspace(vsWorkspace, textBufferFactory, projectionBufferFactory, contentTypeRegistry);
                graph = psharpWorkspace.InsertProjectionBufferGraph(dataModel.DataBuffer);
            }
            return new ProjectionTextViewModel(dataModel, graph);
        }
    }
}
