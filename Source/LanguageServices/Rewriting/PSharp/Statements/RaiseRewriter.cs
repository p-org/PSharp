using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The raise statement rewriter.
    /// </summary>
    internal sealed class RaiseRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RaiseRewriter"/> class.
        /// </summary>
        internal RaiseRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the raise statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is InvocationExpressionSyntax).
                Where(val => (val.Expression as InvocationExpressionSyntax).Expression is IdentifierNameSyntax).
                Where(val => ((val.Expression as InvocationExpressionSyntax).Expression as IdentifierNameSyntax).
                    Identifier.ValueText.Equals("raise")).
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
        /// Rewrites the statement with a raise statement.
        /// </summary>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var invocation = node.Expression as InvocationExpressionSyntax;

            var arguments = new List<ArgumentSyntax>();
            arguments.Add(invocation.ArgumentList.Arguments[0]);

            string payload = string.Empty;
            for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                if (i == invocation.ArgumentList.Arguments.Count - 1)
                {
                    payload += invocation.ArgumentList.Arguments[i].ToString();
                }
                else
                {
                    payload += invocation.ArgumentList.Arguments[i].ToString() + ", ";
                }
            }

            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                "new " + arguments[0].ToString() + "(" + payload + ")"));
            invocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(arguments)));

            var text = node.WithExpression(invocation.WithExpression(SyntaxFactory.IdentifierName("this.Raise"))).ToString();
            this.Program.AddRewrittenTerm(node, text);
            var rewritten = SyntaxFactory.ParseStatement(text).WithTriviaFrom(node);
            return rewritten;
        }
    }
}
