//-----------------------------------------------------------------------
// <copyright file="IgnoreEventsDeclarationVisitor.cs">
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
    /// The P# ignore events declaration parsing visitor.
    /// </summary>
    internal sealed class IgnoreEventsDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal IgnoreEventsDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(StateDeclarationNode parentNode)
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (!expectsComma &&
                    (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent))
                {
                    throw new ParsingException("Expected event identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
                    });
                }

                if (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma)
                {
                    throw new ParsingException("Expected \",\".",
                        new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.EventIdentifier));

                    if (!parentNode.AddIgnoredEvent(base.TokenStream.Peek()))
                    {
                        throw new ParsingException("Unexpected event identifier.",
                            new List<TokenType>());
                    }

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                    base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                {
                    if (!parentNode.AddDeferredEvent(base.TokenStream.Peek()))
                    {
                        throw new ParsingException("Unexpected event identifier.",
                            new List<TokenType>());
                    }

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".",
                    new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }
        }
    }
}
