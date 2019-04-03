// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Field declaration syntax node.
    /// </summary>
    internal class FieldDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The machine parent node.
        /// </summary>
        protected MachineDeclaration Machine;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal readonly AccessModifier AccessModifier;

        /// <summary>
        /// The type identifier.
        /// </summary>
        internal Token TypeIdentifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldDeclaration"/> class.
        /// </summary>
        internal FieldDeclaration(IPSharpProgram program, MachineDeclaration machineNode, ModifierSet modSet)
            : base(program)
        {
            this.Machine = machineNode;
            this.AccessModifier = modSet.AccessModifier;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            string text = string.Empty;

            try
            {
                text = GetIndent(indentLevel) + this.GetRewrittenFieldDeclaration();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit(
                    "Failed to rewrite field '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text,
                    this.Machine.Identifier.TextUnit.Text);
            }

            this.TextUnit = new TextUnit(text, this.TypeIdentifier.TextUnit.Line);
        }

        /// <summary>
        /// Returns the rewritten field declaration.
        /// </summary>
        private string GetRewrittenFieldDeclaration()
        {
            string text = string.Empty;

            if (this.AccessModifier == AccessModifier.Protected)
            {
                text += "protected ";
            }
            else if (this.AccessModifier == AccessModifier.Private)
            {
                text += "private ";
            }

            text += this.TypeIdentifier.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }
    }
}
