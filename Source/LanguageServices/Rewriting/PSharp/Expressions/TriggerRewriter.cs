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
    /// The trigger expression rewriter.
    /// </summary>
    internal sealed class TriggerRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerRewriter"/> class.
        /// </summary>
        internal TriggerRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the trigger expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().
                Where(val => val.Identifier.ValueText.Equals("trigger")).
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
        /// Rewrites the expression with a trigger expression.
        /// </summary>
        private SyntaxNode RewriteExpression(IdentifierNameSyntax node)
        {
            var text = "this.ReceivedEvent";
            this.Program.AddRewrittenTerm(node, text);

            var rewritten = SyntaxFactory.ParseExpression(text).WithTriviaFrom(node);
            return rewritten;
        }
    }
}
