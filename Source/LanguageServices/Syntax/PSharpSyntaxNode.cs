﻿using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// P# syntax node.
    /// </summary>
    internal abstract class PSharpSyntaxNode
    {
        /// <summary>
        /// The program this node belongs to.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// The text unit.
        /// </summary>
        protected internal TextUnit TextUnit { get; protected set; }

        protected const int SpacesPerIndent = 4;
        protected static string OneIndent = new string(' ', SpacesPerIndent);

        /// <summary>
        /// The range of accumulated header tokens.
        /// </summary>
        internal TokenRange HeaderTokenRange;

        /// <summary>
        /// Offset and other information that will be used to create the VS language service
        /// projection buffers for the rewritten form of this declaration.
        /// </summary>
        internal ProjectionNode ProjectionNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpSyntaxNode"/> class.
        /// </summary>
        protected PSharpSyntaxNode(IPSharpProgram program)
        {
            this.Program = program;
            this.ProjectionNode = new ProjectionNode(this);
        }

        /// <summary>
        /// Get the configuration.
        /// </summary>
        protected Configuration Configuration => this.Program.GetProject().CompilationContext.Configuration;

        /// <summary>
        /// Creates a string to be used for the specified level of indentation.
        /// </summary>
        protected static string GetIndent(int indentLevel) => indentLevel == 0 ?
            string.Empty : new System.Text.StringBuilder().Insert(0, OneIndent, indentLevel).ToString();

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal abstract void Rewrite(int indentLevel);
    }
}
