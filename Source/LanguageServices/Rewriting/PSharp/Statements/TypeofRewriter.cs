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
    /// Rewrite typeof statements to fully qualify state names.
    /// </summary>
    internal sealed class TypeofRewriter : PSharpRewriter
    {
        #region fields

        private TypeNameQualifier typeNameQualifier = new TypeNameQualifier();

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal TypeofRewriter(IPSharpProgram program)
            : base(program) {}

        /// <summary>
        /// Rewrites the typeof statements in the program.
        /// </summary>
        /// <param name="rewrittenQualifiedMethods">QualifiedMethods</param>
        internal void Rewrite(HashSet<QualifiedMethod> rewrittenQualifiedMethods)
        {
            this.typeNameQualifier.RewrittenQualifiedMethods = rewrittenQualifiedMethods;

            var typeofnodes = base.Program.GetSyntaxTree().GetRoot().DescendantNodes()
                .OfType<TypeOfExpressionSyntax>().ToList();

            if (typeofnodes.Count > 0)
            {
                var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                    nodes: typeofnodes,
                    computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));
                base.UpdateSyntaxTree(root.ToString());
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the type inside typeof.
        /// </summary>
        /// <param name="node">TypeOfExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(TypeOfExpressionSyntax node)
        {
            this.typeNameQualifier.InitializeForNode(node);

            var fullyQualifiedName = this.typeNameQualifier.GetQualifiedName(node.Type, out var succeeded);
            var rewritten = succeeded
                ? SyntaxFactory.ParseExpression("typeof(" + fullyQualifiedName + ")").WithTriviaFrom(node)
                : node;
            return rewritten;
        }

        #endregion
    }
}
