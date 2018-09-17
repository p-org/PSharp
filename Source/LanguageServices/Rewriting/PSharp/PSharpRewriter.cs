// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// An abstract P# program rewriter.
    /// </summary>
    internal abstract class PSharpRewriter
    {
        #region fields

        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        #endregion

        #region protected API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        protected PSharpRewriter(IPSharpProgram program)
        {
            this.Program = program;
        }

        /// <summary>
        /// Returns the next statement.
        /// </summary>
        /// <param name="node">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        protected SyntaxNode GetNextStatement(SyntaxNode node)
        {
            SyntaxNode next = null;
            var relatives = node.Parent.ChildNodes().ToList();
            for (int idx = 0; idx < relatives.Count; idx++)
            {
                if (relatives[idx].Equals(node) &&
                    idx < relatives.Count - 1)
                {
                    next = relatives[idx + 1];
                    break;
                }
            }

            return next;
        }

        /// <summary>
        /// True if the given syntax node is a machine field.
        /// </summary>
        /// <param name="node">SyntaxNode</param>
        /// <returns>Boolean</returns>
        protected bool IsMachineField(SyntaxNode node)
        {
            var result = false;

            MachineDeclaration machine = null;
            if (!this.TryGetParentMachine(node, out machine))
            {
                return result;
            }

            result = machine.FieldDeclarations.
                Any(s => s.Identifier.TextUnit.Text.Equals(node.ToString()));

            return result;
        }

        /// <summary>
        /// True if the given syntax node is a machine method.
        /// </summary>
        /// <param name="node">SyntaxNode</param>
        /// <returns>Boolean</returns>
        protected bool IsMachineMethod(SyntaxNode node)
        {
            var result = false;

            MachineDeclaration machine = null;
            if (!this.TryGetParentMachine(node, out machine))
            {
                return result;
            }

            result = machine.MethodDeclarations.
                Any(s => s.Identifier.TextUnit.Text.Equals(node.ToString()));

            return result;
        }

        /// <summary>
        /// Tries to return the parent machine identifier, if any.
        /// </summary>
        /// <param name="node">SyntaxNode</param>
        /// <param name="machine">MachineDeclaration</param>
        /// <returns>Boolean</returns>
        protected bool TryGetParentMachine(SyntaxNode node, out MachineDeclaration machine)
        {
            var result = false;
            machine = null;

            var ancestors = node.Ancestors().OfType<ClassDeclarationSyntax>().ToList();
            foreach (var ancestor in ancestors)
            {
                machine = this.Program.GetProject().PSharpPrograms.
                    SelectMany(p => p.NamespaceDeclarations).
                    SelectMany(n => n.MachineDeclarations).
                    FirstOrDefault(s => s.Identifier.TextUnit.Text.Equals(ancestor.Identifier.ValueText));

                if (machine != null)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        /// <param name="text">Text</param>
        protected void UpdateSyntaxTree(string text)
        {
            this.Program.UpdateSyntaxTree(text);
        }

        #endregion
    }
}
