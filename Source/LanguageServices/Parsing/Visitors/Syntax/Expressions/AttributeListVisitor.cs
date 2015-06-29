//-----------------------------------------------------------------------
// <copyright file="AttributeListVisitor.cs">
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

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# attribute list parsing visitor.
    /// </summary>
    internal sealed class AttributeListVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public AttributeListVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits.
        /// </summary>
        public void Visit()
        {
            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightSquareBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit));

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
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
        }
    }
}
