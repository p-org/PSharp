using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The monitor statement rewriter.
    /// </summary>
    internal sealed class MonitorRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorRewriter"/> class.
        /// </summary>
        internal MonitorRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the monitor statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is GenericNameSyntax).
                Where(val => (val.Expression as GenericNameSyntax).Identifier.ValueText.Equals("monitor")).
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
        /// Rewrites the statement with a monitor statement.
        /// </summary>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>();
            arguments.Add(node.ArgumentList.Arguments[0]);

            string payload = string.Empty;
            for (int i = 1; i < node.ArgumentList.Arguments.Count; i++)
            {
                if (i == node.ArgumentList.Arguments.Count - 1)
                {
                    payload += node.ArgumentList.Arguments[i].ToString();
                }
                else
                {
                    payload += node.ArgumentList.Arguments[i].ToString() + ", ";
                }
            }

            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                "new " + arguments[0].ToString() + "(" + payload + ")"));

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression((node.Expression as GenericNameSyntax).
                WithIdentifier(SyntaxFactory.Identifier("this.Monitor")));

            this.Program.AddRewrittenTerm(node, rewritten.ToString());

            rewritten = rewritten.WithTriviaFrom(node);
            return rewritten;
        }
    }
}
