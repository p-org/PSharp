//-----------------------------------------------------------------------
// <copyright file="SeqTypeIdentifierVisitor.cs">
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
    /// The P# seq type identifier parsing visitor.
    /// </summary>
    public sealed class SeqTypeIdentifierVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public SeqTypeIdentifierVisitor(TokenStream tokenStream)
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

            new TypeIdentifierVisitor(base.TokenStream).Visit(node);

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
