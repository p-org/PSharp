﻿// ------------------------------------------------------------------------------------------------
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
        /// The text unit that this token represents.
        /// </summary>
        public readonly TextUnit TextUnit;

        /// <summary>
        /// The text that this token represents.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly TokenType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// By default, the token is <see cref="TokenType.None"/>.
        /// </summary>
        public Token(string text)
        {
            this.Text = text;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// By default, the token is <see cref="TokenType.None"/>.
        /// </summary>
        public Token(TextUnit unit)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(string text, TokenType type)
        {
            this.Text = text;
            this.Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(TextUnit unit, TokenType type)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Type = type;
        }
    }
}
