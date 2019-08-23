// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// An abstract P# program.
    /// </summary>
    public abstract class AbstractPSharpProgram : IPSharpProgram
    {
        /// <summary>
        /// The project that this program belongs to.
        /// </summary>
        protected PSharpProject Project;

        /// <summary>
        /// The syntax tree.
        /// </summary>
        private SyntaxTree SyntaxTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractPSharpProgram"/> class.
        /// </summary>
        internal AbstractPSharpProgram(PSharpProject project, SyntaxTree tree)
        {
            this.Project = project;
            this.SyntaxTree = tree;
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public abstract void Rewrite();

        /// <summary>
        /// Returns the project of the P# program.
        /// </summary>
        public PSharpProject GetProject()
        {
            return this.Project;
        }

        /// <summary>
        /// Returns the syntax tree of the P# program.
        /// </summary>
        public SyntaxTree GetSyntaxTree()
        {
            return this.SyntaxTree;
        }

        /// <summary>
        /// Updates the syntax tree of the P# program.
        /// </summary>
        public void UpdateSyntaxTree(string text)
        {
            var project = this.Project.CompilationContext.GetProjectWithName(this.Project.Name);
            this.SyntaxTree = this.Project.CompilationContext.ReplaceSyntaxTree(this.SyntaxTree, text, project);
        }

        /// <summary>
        /// Add a record of a rewritten term (type, statement, expression) from P# to C#.
        /// </summary>
        public virtual void AddRewrittenTerm(SyntaxNode node, string rewrittenText)
        {
        }

        /// <summary>
        /// Creates a new library using syntax node.
        /// </summary>
        protected static UsingDirectiveSyntax CreateLibrary(string name)
        {
            var leading = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));
            var trailing = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(string.Empty));

            var identifier = SyntaxFactory.Identifier(leading, name, trailing);
            var identifierName = SyntaxFactory.IdentifierName(identifier);

            var usingDirective = SyntaxFactory.UsingDirective(identifierName);
            usingDirective = usingDirective.WithSemicolonToken(usingDirective.SemicolonToken.
                WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("\n"))));

            return usingDirective;
        }
    }
}
