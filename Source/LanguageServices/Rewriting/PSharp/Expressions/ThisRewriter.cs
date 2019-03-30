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
    /// The this expression rewriter.
    /// </summary>
    internal sealed class ThisRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThisRewriter"/> class.
        /// </summary>
        internal ThisRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the this expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().
                OfType<ThisExpressionSyntax>().ToList();

            if (expressions.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: expressions,
                computeReplacementNode: (node, rewritten) => RewriteExpression(rewritten));

            this.UpdateSyntaxTree(root.ToString());
        }

        /// <summary>
        /// Rewrites the expression with a this expression.
        /// </summary>
        private static SyntaxNode RewriteExpression(ThisExpressionSyntax node)
        {
            SyntaxNode rewritten = node;

            if (rewritten.Parent is ArgumentSyntax ||
                (rewritten.Parent is AssignmentExpressionSyntax &&
                (rewritten.Parent as AssignmentExpressionSyntax).Right.IsEquivalentTo(node)))
            {
                var text = "this.Id";

                rewritten = SyntaxFactory.ParseExpression(text);
                rewritten = rewritten.WithTriviaFrom(node);
            }

            return rewritten;
        }
    }
}
