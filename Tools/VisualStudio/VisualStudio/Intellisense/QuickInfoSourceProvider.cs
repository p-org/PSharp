using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.PSharp.VisualStudio
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("P# QuickInfo Source")]
    [Order(Before = "default")]
    [ContentType("psharp")]
    internal class PSharpQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal IBufferTagAggregatorFactoryService AggregatorFactory = null;

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            var tagAggregator = this.AggregatorFactory.CreateTagAggregator<PSharpTokenTag>(textBuffer);
            return new PSharpQuickInfoSource(this, textBuffer, tagAggregator);
        }
    }
}
