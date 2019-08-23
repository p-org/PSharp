// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The goto state statement rewriter.
    /// </summary>
    internal sealed class GotoStateRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GotoStateRewriter"/> class.
        /// Constructor.
        /// </summary>
        internal GotoStateRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the goto state statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is InvocationExpressionSyntax).
                Where(val => (val.Expression as InvocationExpressionSyntax).Expression is IdentifierNameSyntax).
                Where(val => ((val.Expression as InvocationExpressionSyntax).Expression as IdentifierNameSyntax).
                    Identifier.ValueText.Equals("jump")).
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
        /// Rewrites the jump(StateType) statement with a goto&lt;StateType&gt;() statement.
        /// </summary>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var invocation = node.Expression as InvocationExpressionSyntax;

            var arg = invocation.ArgumentList.Arguments[0].ToString();
            var text = node.WithExpression(SyntaxFactory.ParseExpression($"this.Goto<{arg}>()")).ToString();
            this.Program.AddRewrittenTerm(node, text);

            var rewritten = SyntaxFactory.ParseStatement(text).WithTriviaFrom(node);
            return rewritten;
        }
    }
}
