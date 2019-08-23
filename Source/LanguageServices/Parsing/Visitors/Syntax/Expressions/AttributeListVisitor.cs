namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# attribute list parsing visitor.
    /// </summary>
    internal sealed class AttributeListVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeListVisitor"/> class.
        /// </summary>
        public AttributeListVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        public void Visit()
        {
            int counter = 1;
            while (!this.TokenStream.Done)
            {
                if (this.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
                {
                    counter++;
                }
                else if (this.TokenStream.Peek().Type == TokenType.RightSquareBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.RightSquareBracket)
            {
                throw new ParsingException("Expected \"]\".", this.TokenStream.Peek(), TokenType.RightSquareBracket);
            }
        }
    }
}
