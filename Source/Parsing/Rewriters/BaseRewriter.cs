//-----------------------------------------------------------------------
// <copyright file="BaseRewriter.cs">
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

using System.Collections.Generic;
using System.IO;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Abstract rewriter.
    /// </summary>
    internal abstract class BaseRewriter
    {
        #region fields

        /// <summary>
        /// Lines of text to tokenize.
        /// </summary>
        protected List<string> Lines;

        /// <summary>
        /// The current line index.
        /// </summary>
        protected int LineIndex;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text">Text</param>
        public BaseRewriter(string text)
        {
            this.Lines = new List<string>();
            using (StringReader sr = new StringReader(text))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    this.Lines.Add(line);
                }
            }

            this.LineIndex = 0;
        }

        /// <summary>
        /// Tries to get the rewritten text.
        /// </summary>
        /// <param name="result">Rewritten text</param>
        public void TryGet(out string result)
        {
            this.ParseNextLine();

            result = "";
            foreach (var line in this.Lines)
            {
                result += line + "\n";
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parses the next available line.
        /// </summary>
        protected abstract void ParseNextLine();

        #endregion
    }
}
