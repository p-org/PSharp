//-----------------------------------------------------------------------
// <copyright file="BaseParser.cs">
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

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// An abstract parser.
    /// </summary>
    public abstract class BaseParser
    {
        #region fields

        /// <summary>
        /// Syntax tree currently parsed.
        /// </summary>
        protected SyntaxTree SyntaxTree;

        /// <summary>
        /// A P# project.
        /// </summary>
        protected PSharpProject Project;

        /// <summary>
        /// A P# program.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// True if the parser is running internally and not from
        /// visual studio or another external tool.
        /// Else false.
        /// </summary>
        protected bool IsRunningInternally;

        #endregion

        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BaseParser()
        {
            this.IsRunningInternally = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="exitAtError">Exits at error</param>
        internal BaseParser(PSharpProject project, SyntaxTree tree, bool exitAtError)
        {
            this.Project = project;
            this.SyntaxTree = tree;
            this.IsRunningInternally = exitAtError;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns a new P# program.
        /// </summary>
        /// <returns>P# program</returns>
        protected abstract IPSharpProgram CreateNewProgram();

        #endregion
    }
}
