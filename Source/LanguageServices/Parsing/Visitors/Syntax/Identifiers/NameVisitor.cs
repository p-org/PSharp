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
        private readonly TokenRange headerTokenRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="NameVisitor"/> class.
        /// </summary>
        internal NameVisitor(TokenStream tokenStream, TokenRange headerTokenRange = null)
            : base(tokenStream)
            => this.headerTokenRange = headerTokenRange;

        /// <summary>
        /// Consumes a qualified name from the tokenstream.
        /// QN = Identifier || Identifier.QN
        /// </summary>
        internal List<Token> ConsumeQualifiedName(TokenType replacementType)
        {
            var qualifiedName = new List<Token>();

            // Consumes identifier.
            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.", this.TokenStream.Peek(), TokenType.Identifier);
            }

            this.TokenStream.Swap(replacementType);
            qualifiedName.Add(this.TokenStream.Peek());

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!this.TokenStream.Done && this.TokenStream.Peek().Type == TokenType.Dot)
            {
                // Consumes dot.
                qualifiedName.Add(this.TokenStream.Peek());
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // Consumes identifier.
                if (this.TokenStream.Done || this.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected identifier.", this.TokenStream.Peek(), TokenType.Identifier);
                }

                this.TokenStream.Swap(replacementType);
                qualifiedName.Add(this.TokenStream.Peek());

                if (this.headerTokenRange != null)
                {
                    this.headerTokenRange.ExtendStop();
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
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

            TextUnit textUnit = new TextUnit(identifierBuilder.ToString(), eventIdentifiers[0].TextUnit);
            return new Token(textUnit, replacementType);
        }

        /// <summary>
        /// Consumes comma-separated names from the tokenstream.
        /// </summary>
        internal List<List<Token>> ConsumeMultipleNames(TokenType replacement, Func<TokenType, List<Token>> consumeName)
        {
            var names = new List<List<Token>>();

            // Consumes qualified name.
            names.Add(consumeName(replacement));

            while (!this.TokenStream.Done && this.TokenStream.Peek().Type == TokenType.Comma)
            {
                // Consumes comma.
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // Consumes qualified name.
                names.Add(consumeName(replacement));
            }

            return names;
        }

        /// <summary>
        /// Consumes a generic name.
        /// GN = QN || QN LBR matched RBR
        /// </summary>
        internal List<Token> ConsumeGenericName(TokenType replacement)
        {
            // Consumes qualified name.
            var qualifiedName = this.ConsumeQualifiedName(replacement);

            // Consumes template parameters.
            qualifiedName.AddRange(this.ConsumeTemplateParams());

            return qualifiedName;
        }

        /// <summary>
        /// Consumes template parameters: LBR matched RBR.
        /// </summary>
        internal List<Token> ConsumeTemplateParams()
        {
            var templateParams = new List<Token>();

            if (!this.TokenStream.Done && this.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                var balancedCount = 0;

                // Consumes bracket.
                templateParams.Add(this.TokenStream.Peek());
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                // Consumes until matching bracket.
                while (!this.TokenStream.Done)
                {
                    if (this.TokenStream.Peek().Type != TokenType.Identifier &&
                    this.TokenStream.Peek().Type != TokenType.Dot &&
                    this.TokenStream.Peek().Type != TokenType.Comma &&
                    this.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                    this.TokenStream.Peek().Type != TokenType.RightAngleBracket &&
                    this.TokenStream.Peek().Type != TokenType.Object &&
                    this.TokenStream.Peek().Type != TokenType.String &&
                    this.TokenStream.Peek().Type != TokenType.Sbyte &&
                    this.TokenStream.Peek().Type != TokenType.Byte &&
                    this.TokenStream.Peek().Type != TokenType.Short &&
                    this.TokenStream.Peek().Type != TokenType.Ushort &&
                    this.TokenStream.Peek().Type != TokenType.Int &&
                    this.TokenStream.Peek().Type != TokenType.Uint &&
                    this.TokenStream.Peek().Type != TokenType.Long &&
                    this.TokenStream.Peek().Type != TokenType.Ulong &&
                    this.TokenStream.Peek().Type != TokenType.Char &&
                    this.TokenStream.Peek().Type != TokenType.Bool &&
                    this.TokenStream.Peek().Type != TokenType.Decimal &&
                    this.TokenStream.Peek().Type != TokenType.Float &&
                    this.TokenStream.Peek().Type != TokenType.Double)
                    {
                        throw new ParsingException("Unexpected token inside a generic name.", this.TokenStream.Peek(), TokenType.Identifier);
                    }

                    if (this.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        templateParams.Add(this.TokenStream.Peek());
                        this.TokenStream.Index++;
                        this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        if (balancedCount == 0)
                        {
                            // Done.
                            return templateParams;
                        }

                        balancedCount--;
                    }
                    else if (this.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        templateParams.Add(this.TokenStream.Peek());
                        this.TokenStream.Index++;
                        this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                        balancedCount++;
                    }
                    else
                    {
                        templateParams.Add(this.TokenStream.Peek());
                        this.TokenStream.Index++;
                        this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }

                throw new ParsingException("Incomplete generic name.", this.TokenStream.Peek(), TokenType.RightAngleBracket);
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

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.Identifier &&
                this.TokenStream.Peek().Type != TokenType.MulOp &&
                this.TokenStream.Peek().Type != TokenType.HaltEvent &&
                this.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.", this.TokenStream.Peek(),
                    TokenType.Identifier,
                    TokenType.MulOp,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent);
            }

            if (this.TokenStream.Peek().Type == TokenType.MulOp ||
                this.TokenStream.Peek().Type == TokenType.HaltEvent ||
                this.TokenStream.Peek().Type == TokenType.DefaultEvent)
            {
                qualifiedName.Add(this.TokenStream.Peek());
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
            else
            {
                qualifiedName = this.ConsumeGenericName(replacement);
            }

            return qualifiedName;
        }
    }
}
