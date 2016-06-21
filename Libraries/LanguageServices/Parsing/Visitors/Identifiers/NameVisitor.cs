//-----------------------------------------------------------------------
// <copyright file="NameVisitor.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// The P# defer events declaration parsing visitor.
    /// </summary>
    internal sealed class NameVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal NameVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Consume a qualified name from the tokenstream.
        /// QN = Identifier || Identifier.QN
        /// </summary>
        internal List<Token> ConsumeQualifiedName(TokenType Replacement)
        {
            var qualifiedName = new List<Token>();

            // consume identifier
            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                Replacement));
            qualifiedName.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.Dot)
            {
                // consume dot
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // consume identifier
                if (base.TokenStream.Done || base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit, Replacement));
                qualifiedName.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            return qualifiedName;
        }

        /// <summary>
        /// Consume a qualified event name from the tokenstream.
        /// QEN = halt || default || QN
        /// </summary>
        internal List<Token> ConsumeQualifiedEventName(TokenType Replacement)
        {
            var qualifiedName = new List<Token>();

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

            if (base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                base.TokenStream.Peek().Type == TokenType.DefaultEvent)
            {
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
            else
            {
                qualifiedName = ConsumeQualifiedName(Replacement);
            }

            return qualifiedName;
        }


        /// <summary>
        /// Consume comma-separated names from the tokenstream.
        /// </summary>
        internal List<List<Token>> ConsumeMultipleNames(TokenType Replacement, Func<TokenType, List<Token>> ConsumeName)
        {
            var names = new List<List<Token>>();

            // consume qualified name
            names.Add(ConsumeName(Replacement));

            while (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.Comma)
            {
                // consume comma
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // consume qualified name
                names.Add(ConsumeName(Replacement));
            }

            return names;
        }

        /// <summary>
        /// Consume a generic name
        /// GN = QN || QN LBR matched RBR
        /// </summary>
        internal List<Token> ConsumeGenericName(TokenType Replacement)
        {
            // consume qualified name
            var qualifiedName = ConsumeQualifiedName(Replacement);

            if (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                var balanced_count = 0;

                // consume bracket
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // consume until matching bracket
                while (!base.TokenStream.Done)
                {
                    if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.Comma &&
                    base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket &&
                    base.TokenStream.Peek().Type != TokenType.Object &&
                    base.TokenStream.Peek().Type != TokenType.String &&
                    base.TokenStream.Peek().Type != TokenType.Sbyte &&
                    base.TokenStream.Peek().Type != TokenType.Byte &&
                    base.TokenStream.Peek().Type != TokenType.Short &&
                    base.TokenStream.Peek().Type != TokenType.Ushort &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Uint &&
                    base.TokenStream.Peek().Type != TokenType.Long &&
                    base.TokenStream.Peek().Type != TokenType.Ulong &&
                    base.TokenStream.Peek().Type != TokenType.Char &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Decimal &&
                    base.TokenStream.Peek().Type != TokenType.Float &&
                    base.TokenStream.Peek().Type != TokenType.Double)
                    {
                        throw new ParsingException("Unexpected token inside a generic name.",
                            new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                    }

                    if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        qualifiedName.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        if (balanced_count == 0)
                        {
                            // we're done
                            return qualifiedName;
                        }

                        balanced_count--;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        qualifiedName.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        balanced_count++;
                    }
                    else
                    {
                        qualifiedName.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }


                throw new ParsingException("Incomplete generic name.",
                    new List<TokenType>
                {
                            TokenType.RightAngleBracket
                });
            }

            return qualifiedName;
        }

        /// <summary>
        /// Consume a generic event name
        /// GEN = halt || default || GEN 
        /// </summary>
        internal List<Token> ConsumeGenericEventName(TokenType Replacement)
        {
            var qualifiedName = new List<Token>();

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

            if (base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                base.TokenStream.Peek().Type == TokenType.DefaultEvent)
            {
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
            else
            {
                qualifiedName = ConsumeGenericName(Replacement);
            }

            return qualifiedName;
        }
    }
}