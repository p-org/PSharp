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
    internal class TextUnit
    {
        #region fields

        /// <summary>
        /// The text that this text unit represents.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// True if the text unit represents end of a line.
        /// </summary>
        public readonly bool IsEndOfLine;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">Text</param>
        public TextUnit(string text)
        {
            this.Text = text;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isEndOfLine">Is end of line</param>
        public TextUnit(bool isEndOfLine)
        {
            this.Text = "";
            this.IsEndOfLine = isEndOfLine;
        }

        #endregion
    }
}
