// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        /// The parsing options.
        /// </summary>
        protected ParsingOptions Options;

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
