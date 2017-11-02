using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.Linq;

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
        internal ISet<ITrackingSpan> EmbeddedPSharpTrackingSpans { get; set; }

        internal bool IsEmbeddedPSharpPoint(SnapshotPoint snapshotPoint)
            => snapshotPoint.Snapshot.TextBuffer == this.PSharpDiskBuffer ? IsEmbeddedPSharpPoint(snapshotPoint.Position) : false;

        internal bool IsEmbeddedPSharpPoint(int position)
        {
            var offset = position;
            foreach (var sourceSpan in PSharpViewProjTrackingSpans)
            {
                var sourceStartPosition = sourceSpan.GetStartPoint(sourceSpan.TextBuffer.CurrentSnapshot).Position;
                var sourceEndPosition = sourceSpan.GetEndPoint(sourceSpan.TextBuffer.CurrentSnapshot).Position;
                var sourceLength = sourceEndPosition - sourceStartPosition;
                if (offset <= sourceLength)
                {
                    return this.EmbeddedPSharpTrackingSpans.Contains(sourceSpan);
                }
                offset -= sourceLength;
            }
            return false;
        }

        internal static bool GetFromProperties(IPropertyOwner propertyOwner, out ProjectionBufferGraph projectionBufferGraph)
        {
            return propertyOwner.Properties.TryGetProperty(typeof(ProjectionBufferGraph), out projectionBufferGraph);
        }
    }
}
