// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Rewrite typeof statements to fully qualify state names.
    /// </summary>
    internal sealed class GenericTypeRewriter : PSharpRewriter
    {
        private readonly TypeNameQualifier typeNameQualifier = new TypeNameQualifier();

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTypeRewriter"/> class.
        /// </summary>
        internal GenericTypeRewriter(IPSharpProgram program)
            : base(program)
        {
        }

        /// <summary>
        /// Rewrites the typeof statements in the program.
        /// </summary>
        internal void Rewrite(HashSet<QualifiedMethod> rewrittenQualifiedMethods)
        {
            this.typeNameQualifier.RewrittenQualifiedMethods = rewrittenQualifiedMethods;

            var typeofnodes = this.Program.GetSyntaxTree().GetRoot().DescendantNodes()
                .OfType<TypeArgumentListSyntax>().ToList();

            if (typeofnodes.Count > 0)
            {
                var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                    nodes: typeofnodes,
                    computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));
                this.UpdateSyntaxTree(root.ToString());
            }
        }

        /// <summary>
        /// Rewrites the type(s) to qualified names inside a list of generic type arguments.
        /// Primarily intended for the generic method Goto&lt;StateType&gt;().
        /// </summary>
        private SyntaxNode RewriteStatement(TypeArgumentListSyntax node)
        {
            this.typeNameQualifier.InitializeForNode(node);

            var qualifiedNames = node.Arguments.Select(argType => this.typeNameQualifier.GetQualifiedName(argType, out _));
            var qualifiedNamesCsv = string.Join(", ", qualifiedNames);

            var fakeGenericExpression = SyntaxFactory.ParseExpression($"X<{qualifiedNamesCsv}>") as GenericNameSyntax;
            var rewritten = node.WithArguments(fakeGenericExpression.TypeArgumentList.Arguments);
            this.Program.AddRewrittenTerm(node, rewritten.ToString());  // TODO verify this
            return rewritten;
        }
    }
}
