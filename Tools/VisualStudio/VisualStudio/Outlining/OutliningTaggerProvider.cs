using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio.Outlining
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("psharp")]
    internal sealed class OutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
            => GetOrCreateOutliningTagger(textBuffer) as ITagger<T>;

        public static OutliningTagger GetOrCreateOutliningTagger(ITextBuffer textBuffer)
            => textBuffer.Properties.GetOrCreateSingletonProperty(() => new OutliningTagger(textBuffer) as ITagger<IOutliningRegionTag>) as OutliningTagger;
    }
}
