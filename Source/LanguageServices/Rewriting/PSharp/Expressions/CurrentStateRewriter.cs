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
    /// The state expression rewriter.
    /// </summary>
    internal sealed class CurrentStateRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal CurrentStateRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the trigger expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().
                OfType<IdentifierNameSyntax>().
                Where(val => val.Identifier.ValueText.Equals("state")).
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
        /// Rewrites the expression with a trigger expression.
        /// </summary>
        /// <param name="node">IdentifierNameSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteExpression(IdentifierNameSyntax node)
        {
            var text = "this.CurrentState";
            var rewritten = SyntaxFactory.ParseExpression(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
