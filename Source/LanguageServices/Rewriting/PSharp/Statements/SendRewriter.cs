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
    /// The send statement rewriter.
    /// </summary>
    internal sealed class SendRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal SendRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the send statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("send")).
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
        /// Rewrites the statement with a send statement.
        /// </summary>
        /// <param name="node">LocalDeclarationStatementSyntax</param>
        /// <returns>StatementSyntax</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>();
            arguments.Add(node.ArgumentList.Arguments[0]);
            arguments.Add(node.ArgumentList.Arguments[1]);

            string payload = "";
            for (int i = 2; i < node.ArgumentList.Arguments.Count; i++)
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

            arguments[1] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                "new " + arguments[1].ToString() + "(" + payload + ")"));

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression(SyntaxFactory.IdentifierName("this.Send")).
                WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
