﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Namespace declaration syntax node.
    /// </summary>
    internal sealed class NamespaceDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The namespace keyword.
        /// </summary>
        internal Token NamespaceKeyword;

        /// <summary>
        /// The identifier tokens.
        /// </summary>
        internal List<Token> IdentifierTokens;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// List of event declarations.
        /// </summary>
        internal EventDeclarations EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        internal List<MachineDeclaration> MachineDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        /// <summary>
        /// Qualified name of the namespace.
        /// </summary>
        internal string QualifiedName
        {
            get
            {
                return this.IdentifierTokens.Select(t => t.TextUnit.Text).
                    Aggregate("", (acc, name) => acc + name);
            }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        internal NamespaceDeclaration(IPSharpProgram program)
            : base(program)
        {
            this.IdentifierTokens = new List<Token>();
            this.EventDeclarations = new EventDeclarations();
            this.MachineDeclarations = new List<MachineDeclaration>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            var indent = GetIndent(indentLevel); // indent here will likely always be 0
            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }
            
            foreach (var node in this.MachineDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }

            var newLine = string.Empty;
            var text = indent + this.GetRewrittenNamespaceDeclaration(ref newLine);

            var realMachines = this.MachineDeclarations.FindAll(m => !m.IsMonitor);
            var monitors = this.MachineDeclarations.FindAll(m => m.IsMonitor);

            foreach (var node in realMachines)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            foreach (var node in monitors)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            text += indent + this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.NamespaceKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten namespace declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenNamespaceDeclaration(ref string newLine)
        {
            var text = this.NamespaceKeyword.TextUnit.Text;

            text += " ";

            foreach (var token in this.IdentifierTokens)
            {
                text += token.TextUnit.Text;
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.EventDeclarations)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }
            return text;
        }

        #endregion
    }
}
