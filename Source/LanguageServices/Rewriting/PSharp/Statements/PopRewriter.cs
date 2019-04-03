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
    /// The pop statement rewriter.
    /// </summary>
    internal sealed class PopRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopRewriter"/> class.
        /// </summary>
        internal PopRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the pop statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("pop")).
                ToList();

            if (statements.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => RewriteStatement(rewritten));

            this.UpdateSyntaxTree(root.ToString());
        }

        /// <summary>
        /// Rewrites the statement with a pop statement.
        /// </summary>
        private static SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var text = "this.Pop();";

            var rewritten = SyntaxFactory.ParseStatement(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }
    }
}
