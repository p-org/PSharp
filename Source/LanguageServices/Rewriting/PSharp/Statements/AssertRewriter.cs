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
    /// The assert statement rewriter.
    /// </summary>
    internal sealed class AssertRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertRewriter"/> class.
        /// </summary>
        internal AssertRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the assert statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("assert")).
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
        /// Rewrites the statement with a assert statement.
        /// </summary>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var text = "this.Assert";
            this.Program.AddRewrittenTerm(node, text);
            var rewritten = node.WithExpression(SyntaxFactory.IdentifierName(text)).WithTriviaFrom(node);
            return rewritten;
        }
    }
}
