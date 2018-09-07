﻿// ------------------------------------------------------------------------------------------------
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
    /// The create machine expression rewriter.
    /// </summary>
    internal sealed class CreateMachineRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal CreateMachineRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the create machine expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("create")).
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
        /// Rewrites the expression with a create machine expression.
        /// </summary>
        /// <param name="node">InvocationExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>();
            arguments.Add(node.ArgumentList.Arguments[0]);

            if (node.ArgumentList.Arguments.Count > 1)
            {
                arguments.Add(node.ArgumentList.Arguments[1]);
                
                int eventIndex = 1;
                if (arguments[1].ToString().StartsWith("\"") &&
                    arguments[1].ToString().EndsWith("\""))
                {
                    eventIndex++;
                }

                if (node.ArgumentList.Arguments.Count > eventIndex)
                {
                    if (eventIndex == 2)
                    {
                        arguments.Add(node.ArgumentList.Arguments[eventIndex]);
                    }

                    string payload = string.Empty;
                    for (int i = eventIndex + 1; i < node.ArgumentList.Arguments.Count; i++)
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

                    arguments[eventIndex] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                        "new " + arguments[eventIndex].ToString() + "(" + payload + ")"));
                }
            }

            var machineIdentifier = arguments[0].ToString();
            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(
                SyntaxFactory.IdentifierName(machineIdentifier)));

            string text = "this.CreateMachine";

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression(SyntaxFactory.IdentifierName(text)).
                WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
