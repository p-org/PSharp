using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio
{
    class RewrittenSpan
    {
        internal Span OriginalSpan { get; private set; }
        internal ITrackingSpan OriginalTrackingSpan { get; private set; }
        internal Span ReplacementSpan { get; private set; }
        internal ITrackingSpan ReplacementTrackingSpan { get; private set; }

        internal RewrittenTerm RewrittenTerm { get; private set; }

        internal RewrittenSpan(ITextSnapshot origSnapshot, Span origSpan, ITextSnapshot repSnapshot, Span repSpan,
                               RewrittenTerm rewrittenTerm, SpanTrackingMode trackingMode)
        {
            this.OriginalSpan = origSpan;
            this.OriginalTrackingSpan = origSnapshot.CreateTrackingSpan(origSpan, trackingMode);
            this.ReplacementSpan = repSpan;
            this.ReplacementTrackingSpan = repSnapshot.CreateTrackingSpan(repSpan, trackingMode);
            this.RewrittenTerm = rewrittenTerm;
        }
    }
}
