//-----------------------------------------------------------------------
// <copyright file="TextUnit.cs">
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
using System.Diagnostics;
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

        /// <summary>
        /// The source code starting character position of this text unit.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The source code character count of this text unit.
        /// </summary>
        public int Length { get { return this.Text.Length; } }

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">The text of the TextUnit</param>
        /// <param name="line">The line of the TextUnit</param>
        /// <param name="start">The starting position of the TextUnit in the file's text</param>
        public TextUnit(string text, int line, int start)
        {
            this.Text = text;
            this.Line = line;
            this.Start = start;
        }

        /// <summary>
        /// Returns a clone of the text unit.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        /// <returns>TextUnit</returns>
        public static TextUnit Clone(TextUnit textUnit)
        {
            return new TextUnit(textUnit.Text, textUnit.Line, textUnit.Start);
        }

        /// <summary>
        /// Concatenates the two text units, which are assumed to be adjacent.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static TextUnit operator+(TextUnit first, TextUnit second)
        {
            Debug.Assert(first.Start + first.Length == second.Start, "TextUnits not adjacent");
            return new TextUnit(first.Text + second.Text, first.Line, first.Start);
        }

        /// <summary>
        /// Returns a TextUnit with the same Line and Start as the current one but with rewritten text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public TextUnit WithText(string text)
        {
            return new TextUnit(text, this.Line, this.Start);
        }

        /// <summary>
        /// Returns a string representing the TextUnit.
        /// </summary>
        /// <returns>A string representing the TextUnit.</returns>
        public override string ToString()
        {
            return $"#line {this.Line} #char {this.Start}: {this.Text}";
        }
        #endregion
    }
}
