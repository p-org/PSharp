// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml;

namespace Microsoft.PSharp.LanguageServices
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
        protected PSharpProject Project;

        /// <summary>
        /// The syntax tree.
        /// </summary>
        private SyntaxTree SyntaxTree;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        internal AbstractPSharpProgram(PSharpProject project, SyntaxTree tree)
        {
            this.Project = project;
            this.SyntaxTree = tree;
        }

        #endregion

        #region public API

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public abstract void Rewrite();

        /// <summary>
        /// Emits dgml representation of the state machine structure
        /// </summary>
        /// <param name="writer">XmlTestWriter</param>
        public abstract void EmitStateMachineStructure(XmlTextWriter writer);

        /// <summary>
        /// Returns the project of the P# program.
        /// </summary>
        /// <returns>PSharpProject</returns>
        public PSharpProject GetProject()
        {
            return this.Project;
        }

        /// <summary>
        /// Returns the syntax tree of the P# program.
        /// </summary>
        /// <returns>SyntaxTree</returns>
        public SyntaxTree GetSyntaxTree()
        {
            return this.SyntaxTree;
        }

        /// <summary>
        /// Updates the syntax tree of the P# program.
        /// </summary>
        /// <param name="text">Text</param>
        public void UpdateSyntaxTree(string text)
        {
            var project = this.Project.CompilationContext.GetProjectWithName(this.Project.Name);
            this.SyntaxTree = this.Project.CompilationContext.ReplaceSyntaxTree(this.SyntaxTree, text, project);
        }

        #endregion

        #region protected API

        /// <summary>
        /// Creates a new library using syntax node.
        /// </summary>
        /// <param name="name">Library name</param>
        /// <returns>UsingDirectiveSyntax</returns>
        protected UsingDirectiveSyntax CreateLibrary(string name)
        {
            var leading = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
            var trailing = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(""));

            var identifier = SyntaxFactory.Identifier(leading, name, trailing);
            var identifierName = SyntaxFactory.IdentifierName(identifier);

            var usingDirective = SyntaxFactory.UsingDirective(identifierName);
            usingDirective = usingDirective.WithSemicolonToken(usingDirective.SemicolonToken.
                WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("\n"))));

            return usingDirective;
        }

        #endregion
    }
}
