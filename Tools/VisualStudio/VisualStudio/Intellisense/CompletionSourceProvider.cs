using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
#if false // TODO: Requires NotYetImplemented ProjectionTree for performance
    /// <summary>
    /// The P# completion source provider.
    /// </summary>
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("psharp")]
    [Name("token completion")]
    internal class CompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
            => new CompletionSource(this, textBuffer);
    }
#endif
}
