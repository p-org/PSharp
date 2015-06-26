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
