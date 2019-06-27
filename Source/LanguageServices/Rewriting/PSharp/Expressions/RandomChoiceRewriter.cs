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
    /// The random choice expression rewriter.
    /// </summary>
    internal sealed class RandomChoiceRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RandomChoiceRewriter"/> class.
        /// </summary>
        internal RandomChoiceRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the random choice expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().
                OfType<PrefixUnaryExpressionSyntax>().
                Where(val => val.Kind() == SyntaxKind.PointerIndirectionExpression).
                Where(val => val.Parent is IfStatementSyntax).
                ToList();

            if (expressions.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: expressions,
                computeReplacementNode: (node, rewritten) => this.RewriteExpression(rewritten));

            this.UpdateSyntaxTree(root.ToString());
        }

        /// <summary>
        /// Rewrites the expression with a random choice expression.
        /// </summary>
        private SyntaxNode RewriteExpression(PrefixUnaryExpressionSyntax node)
        {
            var text = "this.Random()";
            this.Program.AddRewrittenTerm(node, text);

            var rewritten = SyntaxFactory.ParseExpression(text);    // TODO: .WithTriviaFrom(node); ?
            return rewritten;
        }
    }
}
