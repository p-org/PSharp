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

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# name parsing visitor.
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
        /// Consumes a qualified name from the tokenstream.
        /// QN = Identifier || Identifier.QN
        /// </summary>
        /// <param name="replacementType">TokenType</param>
        /// <returns>Tokens</returns>
        internal List<Token> ConsumeQualifiedName(TokenType replacementType)
        {
            var qualifiedName = new List<Token>();

            // Consumes identifier.
            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.", base.TokenStream.Peek(),
                    TokenType.Identifier);
            }

            base.TokenStream.Swap(replacementType);
            qualifiedName.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.Dot)
            {
                // Consumes dot.
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // Consumes identifier.
                if (base.TokenStream.Done || base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected identifier.", base.TokenStream.Peek(),
                        TokenType.Identifier);
                }

                base.TokenStream.Swap(replacementType);
                qualifiedName.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            return qualifiedName;
        }

        /// <summary>
        /// Consumes a qualified event name from the tokenstream.
        /// QEN = halt || default || * || QN
        /// </summary>
        /// <param name="replacement">TokenType</param>
        /// <returns>Tokens</returns>
        internal List<Token> ConsumeQualifiedEventName(TokenType replacement)
        {
            var qualifiedName = new List<Token>();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.MulOp &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.Identifier,
                        TokenType.MulOp,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
                    });
            }

            if (base.TokenStream.Peek().Type == TokenType.MulOp ||
                base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                base.TokenStream.Peek().Type == TokenType.DefaultEvent)
            {
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
            else
            {
                qualifiedName = ConsumeQualifiedName(replacement);
            }

            return qualifiedName;
        }


        /// <summary>
        /// Consumes comma-separated names from the tokenstream.
        /// </summary>
        /// <param name="replacement">TokenType</param>
        /// <param name="consumeName">Func</param>
        /// <returns>Tokens</returns>
        internal List<List<Token>> ConsumeMultipleNames(TokenType replacement,
            Func<TokenType, List<Token>> consumeName)
        {
            var names = new List<List<Token>>();

            // Consumes qualified name.
            names.Add(consumeName(replacement));

            while (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.Comma)
            {
                // Consumes comma.
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // Consumes qualified name.
                names.Add(consumeName(replacement));
            }

            return names;
        }

        /// <summary>
        /// Consumes a generic name.
        /// GN = QN || QN LBR matched RBR
        /// </summary>
        /// <param name="replacement">TokenType</param>
        /// <returns>Tokens</returns>
        internal List<Token> ConsumeGenericName(TokenType replacement)
        {
            // Consumes qualified name.
            var qualifiedName = ConsumeQualifiedName(replacement);

            if (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                var balancedCount = 0;

                // Consumes bracket.
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // Consumes until matching bracket.
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
                        throw new ParsingException("Unexpected token inside a generic name.", base.TokenStream.Peek(),
                            TokenType.Identifier);
                    }

                    if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        qualifiedName.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        if (balancedCount == 0)
                        {
                            // Done.
                            return qualifiedName;
                        }

                        balancedCount--;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        qualifiedName.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        balancedCount++;
                    }
                    else
                    {
                        qualifiedName.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }


                throw new ParsingException("Incomplete generic name.", base.TokenStream.Peek(),
                    TokenType.RightAngleBracket);
            }

            return qualifiedName;
        }

        /// <summary>
        /// Consumes a generic event name.
        /// GEN = halt || default || * || GEN 
        /// </summary>
        /// <param name="replacement">TokenType</param>
        /// <returns>Tokens</returns>
        internal List<Token> ConsumeGenericEventName(TokenType replacement)
        {
            var qualifiedName = new List<Token>();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.MulOp &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.Identifier,
                        TokenType.MulOp,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
                    });
            }

            if (base.TokenStream.Peek().Type == TokenType.MulOp || 
                base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                base.TokenStream.Peek().Type == TokenType.DefaultEvent)
            {
                qualifiedName.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
            else
            {
                qualifiedName = ConsumeGenericName(replacement);
            }

            return qualifiedName;
        }
    }
}