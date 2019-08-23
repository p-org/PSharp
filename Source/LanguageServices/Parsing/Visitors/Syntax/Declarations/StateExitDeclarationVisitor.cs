using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# state exit declaration parsing visitor.
    /// </summary>
    internal sealed class StateExitDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateExitDeclarationVisitor"/> class.
        /// </summary>
        internal StateExitDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(StateDeclaration parentNode, TokenRange tokenRange, bool isAsync = false)
        {
            if (parentNode.ExitDeclaration != null)
            {
                throw new ParsingException("Duplicate exit declaration.", this.TokenStream.Peek());
            }

            var node = new ExitDeclaration(this.TokenStream.Program, parentNode, isAsync);
            node.ExitKeyword = this.TokenStream.Peek();
            node.HeaderTokenRange = tokenRange.FinishAndClone();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".", this.TokenStream.Peek(), TokenType.LeftCurlyBracket);
            }

            var blockNode = new BlockSyntax(this.TokenStream.Program, parentNode.Machine, parentNode);
            new BlockSyntaxVisitor(this.TokenStream).Visit(blockNode);
            node.StatementBlock = blockNode;

            parentNode.ExitDeclaration = node;
        }
    }
}
