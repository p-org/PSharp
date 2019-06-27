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
        }

        /// <summary>
        /// Returns a clone of the text unit.
        /// </summary>
        public static TextUnit Clone(TextUnit textUnit) => new TextUnit(textUnit.Text, textUnit.Line, textUnit.Start);

        /// <summary>
        /// Concatenates the two text units, which are assumed to be adjacent.
        /// </summary>
        public static TextUnit operator +(TextUnit first, TextUnit second) => Add(first, second);

        /// <summary>
        /// Concatenates the two text units, which are assumed to be adjacent.
        /// </summary>
        public static TextUnit Add(TextUnit first, TextUnit second)
        {
            Debug.Assert(first.Start + first.Length == second.Start, "TextUnits not adjacent");
            return new TextUnit(first.Text + second.Text, first.Line, first.Start);
        }

        /// <summary>
        /// Returns a TextUnit with the same Line and Start as the current one but with rewritten text.
        /// </summary>
        public TextUnit WithText(string text) => new TextUnit(text, this.Line, this.Start);

        /// <summary>
        /// Returns a string representing the TextUnit.
        /// </summary>
        public override string ToString() => $"#line {this.Line} #char {this.Start}: {this.Text}";
    }
}
