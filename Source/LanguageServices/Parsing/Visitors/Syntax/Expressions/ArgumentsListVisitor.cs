// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# argument list parsing visitor.
    /// </summary>
    internal sealed class ArgumentsListVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentsListVisitor"/> class.
        /// </summary>
        internal ArgumentsListVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(ExpressionNode node)
        {
            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            int counter = 1;
            while (!this.TokenStream.Done)
            {
                if (this.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (this.TokenStream.Peek().Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                if (this.TokenStream.Peek().Type == TokenType.NonDeterministic)
                {
                    throw new ParsingException("Can only use the nondeterministic \"$\" keyword as the guard of an if statement.");
                }

                node.StmtTokens.Add(this.TokenStream.Peek());

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                throw new ParsingException("Expected \")\".", TokenType.RightParenthesis);
            }
        }
    }
}
