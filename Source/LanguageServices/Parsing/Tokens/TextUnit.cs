// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A single unit of text.
    /// </summary>
    public class TextUnit
    {
        #region fields

        /// <summary>
        /// The text of this text unit.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The source code line of this text unit.
        /// </summary>
        public readonly int Line;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="line">Line</param>
        public TextUnit(string text, int line)
        {
            this.Text = text;
            this.Line = line;
        }

        /// <summary>
        /// Returns a clone of the text unit.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        /// <returns>TextUnit</returns>
        public static TextUnit Clone(TextUnit textUnit)
        {
            return new TextUnit(textUnit.Text, textUnit.Line);
        }

        #endregion
    }
}
