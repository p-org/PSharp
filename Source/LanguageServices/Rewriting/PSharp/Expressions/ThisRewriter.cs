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

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The this expression rewriter.
    /// </summary>
    internal sealed class ThisRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal ThisRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the this expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().
                OfType<ThisExpressionSyntax>().ToList();

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
        /// Rewrites the expression with a this expression.
        /// </summary>
        /// <param name="node">ThisExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteExpression(ThisExpressionSyntax node)
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

        #endregion
    }
}
