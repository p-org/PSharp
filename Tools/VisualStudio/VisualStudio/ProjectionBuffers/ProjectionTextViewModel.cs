using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Diagnostics;

namespace Microsoft.PSharp.VisualStudio
{
    internal class ProjectionTextViewModel : ITextViewModel
    {
        private readonly ProjectionBufferGraph projectionBufferGraph;

        // The editor source buffer referenced by the EditBuffer.
        public ITextBuffer DataBuffer { get { return this.DataModel.DataBuffer; } }

        public ITextDataModel DataModel { get; private set; }

        // The highest-level P# Projection Buffer.
        public ITextBuffer EditBuffer { get { return projectionBufferGraph.PSharpViewProjectionBuffer; } }

        // Displays the highest-level P# Projection Buffer.
        public ITextBuffer VisualBuffer { get { return projectionBufferGraph.PSharpViewProjectionBuffer; } }

        public PropertyCollection Properties { get; private set; }

        public ProjectionTextViewModel(ITextDataModel dataModel, ProjectionBufferGraph projectionBufferGraph)
        {
            this.DataModel = dataModel;
            this.projectionBufferGraph = projectionBufferGraph;
            this.Properties = new PropertyCollection();
        }

        public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint)
        {
            Debug.Assert(editBufferPoint.Snapshot.TextBuffer == this.VisualBuffer); // TODO
            return editBufferPoint; // TODO make sure these are correct
        }

        public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
        {
            Debug.Assert(editBufferPoint.Snapshot.TextBuffer == this.VisualBuffer); // TODO
            Debug.Assert(targetVisualSnapshot.TextBuffer == this.VisualBuffer);     // TODO
            return editBufferPoint.TranslateTo(targetVisualSnapshot, trackingMode);
        }

        public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity)
        {
            // All non-inert (rewritten) points are in the visual buffer.
            Debug.Assert(editBufferPoint.Snapshot.TextBuffer == this.VisualBuffer); // TODO
            return true;
        }

        public void Dispose()
        {
        }
    }
}
