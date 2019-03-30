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
    /// The halt event type rewriter.
    /// </summary>
    internal sealed class HaltEventRewriter : PSharpRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HaltEventRewriter"/> class.
        /// </summary>
        internal HaltEventRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the halt event types in the program.
        /// </summary>
        internal void Rewrite()
        {
            var types = this.Program.GetSyntaxTree().GetRoot().
                DescendantNodes().OfType<IdentifierNameSyntax>().
                Where(val => val.Identifier.ValueText.Equals("halt")).
                ToList();

            if (types.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: types,
                computeReplacementNode: (node, rewritten) => RewriteType(rewritten));

            this.UpdateSyntaxTree(root.ToString());
        }

        /// <summary>
        /// Rewrites the type with a halt event type.
        /// </summary>
        private static ExpressionSyntax RewriteType(IdentifierNameSyntax node)
        {
            var text = typeof(Halt).FullName;

            var rewritten = SyntaxFactory.ParseName(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }
    }
}
