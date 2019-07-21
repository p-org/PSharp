// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// P# syntax token.
    /// </summary>
    public sealed class Token
    {
        /// <summary>
        /// The text unit that this token represents in the rewritten buffer.
        /// </summary>
        public readonly TextUnit TextUnit;

        /// <summary>
        /// The text that this token represents.
        /// </summary>
        public string Text => this.TextUnit.Text;

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly TokenType Type;

        /// <summary>
        /// The default type of a token.
        /// </summary>
        public const TokenType DefaultTokenType = TokenType.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(TextUnit unit, TokenType type = DefaultTokenType)
        {
            this.TextUnit = unit;
            this.Type = type;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Token"/> class with updated type
        /// </summary>
        public Token WithType(TokenType updatedType) => new Token(this.TextUnit, updatedType);

        /// <summary>
        /// Returns a string representing the Token.
        /// </summary>
        /// <returns>A string representing the TextUnit.</returns>
        public override string ToString()
        {
            return $"{this.Text} {this.Type} ({this.TextUnit})";
        }
    }
}
