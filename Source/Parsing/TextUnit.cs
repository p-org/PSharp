//-----------------------------------------------------------------------
// <copyright file="TextUnit.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Parsing
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
        /// The length of this text unit.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// The starting point of this text unit.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The end point of this text unit.
        /// </summary>
        public readonly int End;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="position">Starting position</param>
        public TextUnit(string text, int position)
        {
            this.Text = text;
            this.Length = text.Length;
            this.Start = position;
            this.End = position + text.Length - 1;
        }

        /// <summary>
        /// Returns a clone of the text unit.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        /// <param name="position">Position</param>
        /// <returns>TextUnit</returns>
        public static TextUnit Clone(TextUnit textUnit, int position)
        {
            return new TextUnit(textUnit.Text, position);
        }

        #endregion
    }
}
