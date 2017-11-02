using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.VisualStudio
{
    internal class PSharpQuickInfoController : IIntellisenseController
    {
        private ITextView textView;
        private IList<ITextBuffer> subjectBuffers;
        private ITextBuffer csharpBuffer;
        private PSharpQuickInfoControllerProvider provider;
        private IQuickInfoSession session;

        internal PSharpQuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, PSharpQuickInfoControllerProvider provider)
        {
            this.textView = textView;
            this.subjectBuffers = subjectBuffers;
            this.provider = provider;

            this.csharpBuffer = GetCSharpBuffer(textView.BufferGraph.TopBuffer as IProjectionBuffer);
            if (this.csharpBuffer != null)
            {
                this.textView.MouseHover += this.OnTextViewMouseHover;
            }
        }

        internal static ITextBuffer GetCSharpBuffer(ITextBuffer topBuffer)
        {
            var projectionBuffer = topBuffer as IProjectionBuffer;
            return projectionBuffer?.SourceBuffers.FirstOrDefault(buf => buf.ContentType.TypeName.ToLower() == "csharp");
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            if (this.provider.QuickInfoBroker.IsQuickInfoActive(this.textView))
            {
                return;
            }
            var mousePoint = new SnapshotPoint(this.textView.TextSnapshot, e.Position);

            // If the mouse position maps to the csharp buffer then it should be handled by C# only.
            SnapshotPoint? csharpPoint = this.textView.BufferGraph.MapDownToBuffer(
                mousePoint, PointTrackingMode.Positive, this.csharpBuffer, PositionAffinity.Predecessor);
            if (csharpPoint != null)
            {
                return;
            }

            // Find the mouse position by mapping down to the subject (P#) buffer
            SnapshotPoint? point = this.textView.BufferGraph.MapDownToFirstMatch(
                mousePoint, PointTrackingMode.Positive, snapshot => this.subjectBuffers.Contains(snapshot.TextBuffer), PositionAffinity.Predecessor);
            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);
                this.session = this.provider.QuickInfoBroker.TriggerQuickInfo(this.textView, triggerPoint, true);
            }
        }

        public void Detach(ITextView textView)
        {
            if (this.textView == textView)
            {
                this.textView.MouseHover -= this.OnTextViewMouseHover;
                this.textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            // TODO: ConnectSubjectBuffer unused
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            // TODO: ConnectSubjectBuffer unused
        }
    }
}
