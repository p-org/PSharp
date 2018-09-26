// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal RandomChoiceRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the random choice expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().
                OfType<PrefixUnaryExpressionSyntax>().
                Where(val => val.Kind() == SyntaxKind.PointerIndirectionExpression).
                Where(val => val.Parent is IfStatementSyntax).
                ToList();

            if (expressions.Count == 0)
            {
                return;
            }

            var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: expressions,
                computeReplacementNode: (node, rewritten) => this.RewriteExpression(rewritten));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the expression with a random choice expression.
        /// </summary>
        /// <param name="node">PrefixUnaryExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteExpression(PrefixUnaryExpressionSyntax node)
        {
            var text = "this.Random()";
            var rewritten = SyntaxFactory.ParseExpression(text);
            return rewritten;
        }

        #endregion
    }
}
