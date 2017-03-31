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

using System;

using Microsoft.CodeAnalysis;
using Microsoft.PSharp.IO;

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
        /// The parsing options.
        /// </summary>
        protected ParsingOptions Options;

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected ILogger Logger;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">ParsingOptions</param>
        public BaseParser(ParsingOptions options)
        {
            this.Options = options;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
        internal BaseParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
        {
            this.Project = project;
            this.SyntaxTree = tree;
            this.Options = options;
        }

        /// <summary>
        /// Initialized the parser.
        /// </summary>
        private void Initialize()
        {
            this.Logger = new DefaultLogger();
        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns a new P# program.
        /// </summary>
        /// <returns>P# program</returns>
        protected abstract IPSharpProgram CreateNewProgram();

        #endregion

        #region logging

        /// <summary>
        /// Installs the specified <see cref="ILogger"/>.
        /// </summary>
        /// <param name="logger">TextWriter</param>
        public void SetLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new InvalidOperationException("Cannot install a null logger.");
            }

            this.Logger.Dispose();
            this.Logger = logger;
        }

        #endregion
    }
}
