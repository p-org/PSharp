﻿// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// The pop statement rewriter.
    /// </summary>
    internal sealed class PopRewriter : CSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopRewriter"/> class.
        /// </summary>
        internal PopRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the program.
        /// </summary>
        public override void Rewrite()
        {
            var compilation = this.Program.GetProject().GetCompilation();
            var model = compilation.GetSemanticModel(this.Program.GetSyntaxTree());

            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is InvocationExpressionSyntax).
                Where(val => IsExpectedExpression(val.Expression, "Microsoft.PSharp.Pop", model)).
                ToList();

            if (statements.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            this.UpdateSyntaxTree(root.ToString());
        }

        /// <summary>
        /// Rewrites the pop statement.
        /// </summary>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var text = "{ " + node.ToString() + "return; }";
            var rewritten = SyntaxFactory.ParseStatement(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }
    }
}
