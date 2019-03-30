// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        /// Initializes a new instance of the <see cref="TextUnit"/> class.
        /// </summary>
        public TextUnit(string text, int line)
        {
            this.Text = text;
            this.Line = line;
        }

        /// <summary>
        /// Returns a clone of the text unit.
        /// </summary>
        public static TextUnit Clone(TextUnit textUnit) => new TextUnit(textUnit.Text, textUnit.Line);
    }
}
