//-----------------------------------------------------------------------
// <copyright file="PayloadCollectionVisitor.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# payload collection parsing visitor.
    /// </summary>
    internal sealed class PayloadCollectionVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal PayloadCollectionVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="node">Node</param>
        internal void Visit(PPayloadSendExpressionNode node)
        {
            node.StmtTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightSquareBracket)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.This &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.True &&
                    base.TokenStream.Peek().Type != TokenType.False &&
                    base.TokenStream.Peek().Type != TokenType.Null &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    base.TokenStream.Peek().Type != TokenType.LeftSquareBracket) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.This ||
                    base.TokenStream.Peek().Type == TokenType.Identifier ||
                    base.TokenStream.Peek().Type == TokenType.True ||
                    base.TokenStream.Peek().Type == TokenType.False ||
                    base.TokenStream.Peek().Type == TokenType.Null ||
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis ||
                    base.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
                {
                    new PayloadVisitor(base.TokenStream).Visit(node);
                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    node.StmtTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    expectsComma = false;
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightSquareBracket)
            {
                throw new ParsingException("Expected \"]\".",
                    new List<TokenType>
                {
                    TokenType.RightSquareBracket
                });
            }

            node.StmtTokens.Add(base.TokenStream.Peek());
        }
    }
}
