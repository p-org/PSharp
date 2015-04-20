//-----------------------------------------------------------------------
// <copyright file="AbstractPSharpProgram.cs">
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

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// An abstract P# program.
    /// </summary>
    public abstract class AbstractPSharpProgram : IPSharpProgram
    {
        #region fields

        /// <summary>
        /// The rewritten text.
        /// </summary>
        protected string RewrittenText;

        /// <summary>
        /// File path of the P# program.
        /// </summary>
        protected readonly string FilePath;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        public AbstractPSharpProgram(string filePath)
        {
            this.RewrittenText = "";
            this.FilePath = filePath;
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public abstract string Rewrite();

        /// <summary>
        /// Returns the full text of this P# program.
        /// </summary>
        /// <returns>Full text</returns>
        public abstract string GetFullText();

        /// <summary>
        /// Returns the rewritten to C#-IR text of this P# program.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public string GetRewrittenText()
        {
            return this.RewrittenText;
        }

        /// <summary>
        /// Generates the text units of this P# program.
        /// </summary>
        public abstract void GenerateTextUnits();

        #endregion

        #region protected API

        /// <summary>
        /// Instrument the system library.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Text</returns>
        protected string InstrumentSystemLibrary(ref int position)
        {
            var text = "using System;\n";
            position += text.Length;
            return text;
        }

        /// <summary>
        /// Instrument the system generic collections library.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Text</returns>
        protected string InstrumentSystemCollectionsGenericLibrary(ref int position)
        {
            var text = "using System.Collections.Generic;\n";
            position += text.Length;
            return text;
        }

        /// <summary>
        /// Instrument the P# library.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Text</returns>
        protected string InstrumentPSharpLibrary(ref int position)
        {
            var text = "using Microsoft.PSharp;\n";
            position += text.Length;
            return text;
        }

        /// <summary>
        /// Instrument the P# collections library.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Text</returns>
        protected string InstrumentPSharpCollectionsLibrary(ref int position)
        {
            var text = "using Microsoft.PSharp.Collections;\n";
            position += text.Length;
            return text;
        }

        #endregion
    }
}
