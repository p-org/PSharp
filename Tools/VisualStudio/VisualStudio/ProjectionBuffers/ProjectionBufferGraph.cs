using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.PSharp.VisualStudio
{
    class ProjectionBufferGraph
    {
        internal IProjectionBuffer PSharpViewProjectionBuffer { get; set; }
        internal IProjectionBuffer CSharpProjectionBuffer { get; set; }
        internal Document CSharpProjectionDocument { get; set; }    // TODO needed?
        internal ITextBuffer PSharpDiskBuffer { get; set; }
        internal ITrackingSpan[] CSharpProjTrackingSpans { get; set; }
        internal ITrackingSpan[] PSharpViewProjTrackingSpans { get; set; }
    }
}
