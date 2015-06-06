//-----------------------------------------------------------------------
// <copyright file="ArgumentsListVisitor.cs">
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
    /// The P# argument list parsing visitor.
    /// </summary>
    internal sealed class ArgumentsListVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal ArgumentsListVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="node">Node</param>
        internal void Visit(ExpressionNode node)
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.NonDeterministic)
                {
                    throw new ParsingException("Can only use the nondeterministic \"$\" " +
                        "keyword as the guard of an if statement.", new List<TokenType>());
                }

                node.StmtTokens.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                throw new ParsingException("Expected \")\".",
                    new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }
        }
    }
}
