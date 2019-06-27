// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The push state statement rewriter.
    /// </summary>
    internal sealed class PushStateRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PushStateRewriter"/> class.
        /// </summary>
        internal PushStateRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the push state statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is InvocationExpressionSyntax).
                Where(val => (val.Expression as InvocationExpressionSyntax).Expression is IdentifierNameSyntax).
                Where(val => ((val.Expression as InvocationExpressionSyntax).Expression as IdentifierNameSyntax).
                    Identifier.ValueText.Equals("push")).
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
        /// Rewrites the push(StateType) statement with a push&lt;StateType&gt;() statement.
        /// </summary>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var invocation = node.Expression as InvocationExpressionSyntax;

            var arg = invocation.ArgumentList.Arguments[0].ToString();
            var text = node.WithExpression(SyntaxFactory.ParseExpression($"this.Push<{arg}>()")).ToString();
            this.Program.AddRewrittenTerm(node, text);

            var rewritten = SyntaxFactory.ParseStatement(text).WithTriviaFrom(node);
            return rewritten;
        }
    }
}
