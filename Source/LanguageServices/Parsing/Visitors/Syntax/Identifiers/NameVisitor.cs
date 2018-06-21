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
using System.Text;

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
        /// <param name="replacement">TokenType</param>
        /// <returns>Tokens</returns>
        internal List<Token> ConsumeQualifiedName(TokenType replacement)
        {
            var qualifiedName = new List<Token>();

            // Consumes identifier.
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
                replacement));
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
                    throw new ParsingException("Expected identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit, replacement));
                qualifiedName.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            return qualifiedName;
        }

        internal static Token VisitSimpleQualifiedName(TokenStream tokenStream, TokenType replacementType)
        {
            var eventIdentifiers = new NameVisitor(tokenStream).ConsumeQualifiedName(replacementType);
            var identifierBuilder = new StringBuilder();
            foreach (var token in eventIdentifiers)
            {
                identifierBuilder.Append(token.TextUnit.Text);
            }

            TextUnit textUnit = new TextUnit(identifierBuilder.ToString(), eventIdentifiers[0].TextUnit.Line);
            return new Token(textUnit, replacementType);
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
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
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

            // consures template parameters
            qualifiedName.AddRange(ConsumeTemplateParams());

            return qualifiedName;
        }

        /// <summary>
        /// Consumes template parameters:
        /// LBR matched RBR
        /// </summary>
        /// <returns>Tokens</returns>
        internal List<Token> ConsumeTemplateParams()
        {
            var templateParams = new List<Token>();

            if (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                var balancedCount = 0;

                // Consumes bracket.
                templateParams.Add(base.TokenStream.Peek());
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
                        throw new ParsingException("Unexpected token inside a generic name.",
                            new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                    }

                    if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        templateParams.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        if (balancedCount == 0)
                        {
                            // Done.
                            return templateParams;
                        }

                        balancedCount--;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        templateParams.Add(base.TokenStream.Peek());
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        balancedCount++;
                    }
                    else
                    {
                        templateParams.Add(base.TokenStream.Peek());
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

            return templateParams;
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
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
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