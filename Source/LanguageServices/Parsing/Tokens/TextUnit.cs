// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------
using System.Diagnostics;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A single unit of text.
    /// </summary>
    public class TextUnit
    {
        /// <summary>
        /// The text of this text unit.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The source code line of this text unit.
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// The source code starting character position of this text unit.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The source code character count of this text unit.
        /// </summary>
        public int Length => this.Text.Length;

        /// <summary>
        /// Sometimes we replace text so need this value
        /// </summary>
        public int OriginalLength { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextUnit"/> class.
        /// </summary>
        public TextUnit(string text, TextUnit other)
            : this(text, other.Line, other.Start)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextUnit"/> class.
        /// </summary>
        public TextUnit(string text, int line, int start)
        {
            this.Text = text;
            this.Line = line;
            this.Start = start;
            this.OriginalLength = text.Length;
        }

        /// <summary>
        /// Concatenates the two text units, which are assumed to be adjacent.
        /// </summary>
        public static TextUnit operator +(TextUnit first, TextUnit second) => Add(first, second);

        /// <summary>
        /// Concatenates the two text units, which are assumed to be adjacent.
        /// </summary>
        public static TextUnit Add(TextUnit first, TextUnit second)
        {
            // We call SkipWhiteSpaceAndCommentTokens() between some tokens so the first may not end right at the second.
            Debug.Assert(first.Start + first.OriginalLength <= second.Start, "TextUnits overlap");
            return Create(first.Text + second.Text, first.OriginalLength + second.OriginalLength, first.Line, first.Start);
        }

        private static TextUnit Create(string text, int originalLength, int line, int start)
            => new TextUnit(text, line, start)
            {
                OriginalLength = originalLength
            };

        /// <summary>
        /// Returns a TextUnit with the same Line and Start as the current one but with rewritten text.
        /// </summary>
        public TextUnit WithText(string text) => Create(text, this.OriginalLength, this.Line, this.Start);

        /// <summary>
        /// Returns a string representing the TextUnit.
        /// </summary>
        public override string ToString() => $"#line {this.Line} #char {this.Start}: {this.Text}";
    }
}
