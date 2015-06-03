//-----------------------------------------------------------------------
// <copyright file="MapTypeIdentifierVisitor.cs">
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
    /// The P# map type identifier parsing visitor.
    /// </summary>
    public sealed class MapTypeIdentifierVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public MapTypeIdentifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="node">Node</param>
        public void Visit(PTypeNode node)
        {
            node.TypeTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftSquareBracket)
            {
                throw new ParsingException("Expected \"[\".",
                    new List<TokenType>
                {
                    TokenType.LeftSquareBracket
                });
            }

            node.TypeTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Seq ||
                    base.TokenStream.Peek().Type == TokenType.Map ||
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    new TypeIdentifierVisitor(base.TokenStream).Visit(node);
                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    node.TypeTokens.Add(base.TokenStream.Peek());

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

            node.TypeTokens.Add(base.TokenStream.Peek());
        }
    }
}
