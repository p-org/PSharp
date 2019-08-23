// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Anonymous action handler syntax node.
    /// </summary>
    internal class AnonymousActionHandler
    {
        /// <summary>
        /// The block containing the handler statements.
        /// </summary>
        internal readonly BlockSyntax BlockSyntax;

        /// <summary>
        /// Indicates whether the generated method should be 'async Task'.
        /// </summary>
        internal readonly bool IsAsync;

        internal AnonymousActionHandler(BlockSyntax blockSyntax, bool isAsync)
        {
            this.BlockSyntax = blockSyntax;
            this.IsAsync = isAsync;
        }
    }
}
