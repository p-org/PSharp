using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.PSharp.VisualStudio.BraceMatching
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("psharp")]
    [TagType(typeof(TextMarkerTag))]
    internal sealed class BraceMatchingTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Provide highlighting only on the top-level buffer
            return textView == null || textView.TextBuffer != buffer
                ? null
                : new BraceMatchingTagger(textView, buffer) as ITagger<T>;
        }
    }
}
