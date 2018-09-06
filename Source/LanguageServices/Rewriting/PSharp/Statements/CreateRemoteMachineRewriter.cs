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
    /// The create remote machine expression rewriter.
    /// </summary>
    internal sealed class CreateRemoteMachineRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal CreateRemoteMachineRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the create remote machine expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("remote")).
                ToList();

            if (statements.Count == 0)
            {
                return;
            }

            var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the expression with a create remote machine expression.
        /// </summary>
        /// <param name="node">InvocationExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>(node.ArgumentList.Arguments);
            arguments.Add(node.ArgumentList.Arguments[0]);
            arguments.Add(node.ArgumentList.Arguments[1]);

            if (node.ArgumentList.Arguments.Count > 2)
            {
                arguments.Add(node.ArgumentList.Arguments[2]);

                string payload = "";
                for (int i = 3; i < node.ArgumentList.Arguments.Count; i++)
                {
                    if (i == node.ArgumentList.Arguments.Count - 1)
                    {
                        payload += node.ArgumentList.Arguments[i].ToString();
                    }
                    else
                    {
                        payload += node.ArgumentList.Arguments[i].ToString() + ", ";
                    }
                }

                arguments[2] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                    "new " + arguments[2].ToString() + "(" + payload + ")"));
            }

            var machineIdentifier = arguments[0].ToString();
            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(
                SyntaxFactory.IdentifierName(machineIdentifier)));

            var text = "this.CreateRemoteMachine";

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression(SyntaxFactory.IdentifierName(text)).
                WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
