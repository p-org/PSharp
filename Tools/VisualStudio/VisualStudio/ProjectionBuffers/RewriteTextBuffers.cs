using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;
using System.Linq;

namespace Microsoft.PSharp.VisualStudio
{
    class RewriteTextBuffers
    {
        private static ITextBufferFactoryService textBufferFactory = null;
        private static IContentType psharpContentType;
        private static IContentType inertContentType;

        internal static SpanTrackingMode SpanTrackingMode = SpanTrackingMode.EdgeExclusive; // TODO is this the right option

        internal static void Initialize(ITextBufferFactoryService tbf, IContentType pct, IContentType ict)
        {
            textBufferFactory = tbf;
            psharpContentType = pct;
            inertContentType = ict;
        }

        internal static RewrittenSpan[] GetRewrittenSpans(RewrittenTerms rewrittenTerms)
        {
            // We must have a different source buffer for each instance of each string (that is, we can't
            // reuse the same span for "MachineId", for example) as this gives an overlap error. Therefore,
            // we'll use one textBuffer for each replacement. TODO evaluate performance and perhaps merge.
            RewrittenSpan createRewrittenSpan(RewrittenTerm rewrittenTerm)
            {
                var origTextBuffer = textBufferFactory.CreateTextBuffer(rewrittenTerm.OriginalString, psharpContentType);
                var rewrittenTextBuffer = textBufferFactory.CreateTextBuffer(rewrittenTerm.RewrittenString, inertContentType);
                return new RewrittenSpan(origTextBuffer.CurrentSnapshot, new Span(0, rewrittenTerm.OriginalLength),
                                         rewrittenTextBuffer.CurrentSnapshot, new Span(0, rewrittenTerm.RewrittenLength),
                                         rewrittenTerm, SpanTrackingMode);
            }
            return rewrittenTerms.Select(term => createRewrittenSpan(term)).ToArray();
        }
    }
}
