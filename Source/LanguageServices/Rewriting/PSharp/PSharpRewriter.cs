// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// An abstract P# program rewriter.
    /// </summary>
    internal abstract class PSharpRewriter
    {
        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpRewriter"/> class.
        /// </summary>
        protected PSharpRewriter(IPSharpProgram program) => this.Program = program;

#if false // TODO remove if not used
        /// <summary>
        /// Returns the next statement.
        /// </summary>
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
        protected bool IsMachineField(SyntaxNode node)
            => this.TryGetParentMachine(node, out MachineDeclaration machine)
                ? machine.FieldDeclarations.Any(s => s.Identifier.TextUnit.Text.Equals(node.ToString()))
                : false;

        /// <summary>
        /// True if the given syntax node is a machine method.
        /// </summary>
        protected bool IsMachineMethod(SyntaxNode node)
            => this.TryGetParentMachine(node, out MachineDeclaration machine)
                ? machine.MethodDeclarations.Any(s => s.Identifier.TextUnit.Text.Equals(node.ToString()))
                : false;

        /// <summary>
        /// Tries to return the parent machine identifier, if any.
        /// </summary>
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
#endif // not used

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        protected void UpdateSyntaxTree(string text) => this.Program.UpdateSyntaxTree(text);
    }
}
