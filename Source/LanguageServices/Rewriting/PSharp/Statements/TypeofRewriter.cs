﻿using System.Collections.Generic;
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
        private readonly TypeNameQualifier typeNameQualifier = new TypeNameQualifier();

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeofRewriter"/> class.
        /// </summary>
        internal TypeofRewriter(IPSharpProgram program)
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
                .OfType<TypeOfExpressionSyntax>().ToList();

            if (typeofnodes.Count > 0)
            {
                var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                    nodes: typeofnodes,
                    computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));
                this.UpdateSyntaxTree(root.ToString());
            }
        }

        /// <summary>
        /// Rewrites the type inside typeof.
        /// </summary>
        private SyntaxNode RewriteStatement(TypeOfExpressionSyntax node)
        {
            this.typeNameQualifier.InitializeForNode(node);

            var fullyQualifiedName = this.typeNameQualifier.GetQualifiedName(node.Type, out var succeeded);
            if (!succeeded)
            {
                return node;
            }

            var rewritten = SyntaxFactory.ParseExpression("typeof(" + fullyQualifiedName + ")");
            this.Program.AddRewrittenTerm(node, rewritten.ToString());

            rewritten = rewritten.WithTriviaFrom(node);
            return rewritten;
        }
    }
}
