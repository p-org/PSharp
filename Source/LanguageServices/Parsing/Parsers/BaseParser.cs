// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// An abstract parser.
    /// </summary>
    public abstract class BaseParser
    {
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
        /// Initializes a new instance of the <see cref="BaseParser"/> class.
        /// </summary>
        public BaseParser(ParsingOptions options)
        {
            this.Options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseParser"/> class.
        /// </summary>
        internal BaseParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
        {
            this.Project = project;
            this.SyntaxTree = tree;
            this.Options = options;
        }

        /// <summary>
        /// Returns a new P# program.
        /// </summary>
        protected abstract IPSharpProgram CreateNewProgram();
    }
}
