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
        /// The project that this program belongs to.
        /// </summary>
        internal PSharpProject Project;

        /// <summary>
        /// The rewritten text.
        /// </summary>
        internal protected string RewrittenText
        {
            get; protected set;
        }

        /// <summary>
        /// The model text.
        /// </summary>
        internal protected string ModelText
        {
            get; protected set;
        }

        /// <summary>
        /// File path of the P# program.
        /// </summary>
        protected readonly string FilePath;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="filePath">File path</param>
        internal AbstractPSharpProgram(PSharpProject project, string filePath)
        {
            this.Project = project;
            this.RewrittenText = "";
            this.ModelText = "";
            this.FilePath = filePath;
        }

        #endregion

        #region public API

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        public abstract string Rewrite();

        /// <summary>
        /// Models the P# program to the C#-IR.
        /// </summary>
        /// <returns>Modeled text</returns>
        public abstract string Model();

        #endregion

        #region protected API

        /// <summary>
        /// Instrument the system library.
        /// </summary>
        /// <returns>Text</returns>
        protected string InstrumentSystemLibrary()
        {
            var text = "using System;\n";
            return text;
        }

        /// <summary>
        /// Instrument the system generic collections library.
        /// </summary>
        /// <returns>Text</returns>
        protected string InstrumentSystemCollectionsGenericLibrary()
        {
            var text = "using System.Collections.Generic;\n";
            return text;
        }

        /// <summary>
        /// Instrument the P# library.
        /// </summary>
        /// <returns>Text</returns>
        protected string InstrumentPSharpLibrary()
        {
            var text = "using Microsoft.PSharp;\n";
            return text;
        }

        /// <summary>
        /// Instrument the P# collections library.
        /// </summary>
        /// <returns>Text</returns>
        protected string InstrumentPSharpCollectionsLibrary()
        {
            var text = "using Microsoft.PSharp.Collections;\n";
            return text;
        }

        #endregion
    }
}
