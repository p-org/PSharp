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
        #region fields

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

        #endregion

        #region public API

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="text">String</param>
        public Token(string text)
        {
            this.Text = text;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        public Token(TextUnit unit)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">String</param>
        /// <param name="type">TokenType</param>
        public Token(string text, TokenType type)
        {
            this.Text = text;
            this.Type = type;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        /// <param name="type">TokenType</param>
        public Token(TextUnit unit, TokenType type)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Type = type;
        }

        #endregion
    }
}
