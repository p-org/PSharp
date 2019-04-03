// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// State declaration syntax node.
    /// </summary>
    internal sealed class StateGroupDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

        /// <summary>
        /// Parent group declaration node (if any).
        /// </summary>
        internal readonly StateGroupDeclaration Group;

        /// <summary>
        /// The state group keyword.
        /// </summary>
        internal Token StateGroupKeyword;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// Nested state declarations.
        /// </summary>
        internal List<StateDeclaration> StateDeclarations;

        /// <summary>
        /// Nested state group declarations.
        /// </summary>
        internal List<StateGroupDeclaration> StateGroupDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateGroupDeclaration"/> class.
        /// </summary>
        internal StateGroupDeclaration(IPSharpProgram program, MachineDeclaration machineNode,
            StateGroupDeclaration groupNode)
            : base(program)
        {
            this.Machine = machineNode;
            this.Group = groupNode;
            this.StateDeclarations = new List<StateDeclaration>();
            this.StateGroupDeclarations = new List<StateGroupDeclaration>();
        }

        /// <summary>
        /// Returns all state declarations inside this group (recursively).
        /// </summary>
        internal List<StateDeclaration> GetAllStateDeclarations()
        {
            var decls = new List<StateDeclaration>();
            decls.AddRange(this.StateDeclarations);
            this.StateGroupDeclarations.ForEach(g => decls.AddRange(g.GetAllStateDeclarations()));
            return decls;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            string text = string.Empty;
            foreach (var node in this.StateGroupDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }

            foreach (var node in this.StateDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }

            try
            {
                text = this.GetRewrittenStateGroupDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit(
                    "Failed to rewrite state group '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text,
                    this.Machine.Identifier.TextUnit.Text);
            }

            text += GetIndent(indentLevel) + this.RightCurlyBracketToken.TextUnit.Text + "\n";

            this.TextUnit = new TextUnit(text, this.StateGroupKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Sanity checking: no duplicate states and groups.
        /// </summary>
        internal void CheckDeclaration()
        {
            var statesSeen = new Dictionary<string, StateDeclaration>();
            foreach (var decl in this.StateDeclarations)
            {
                if (statesSeen.ContainsKey(decl.Identifier.Text))
                {
                    throw new RewritingException(
                        $"Multiple declarations of the state '{decl.Identifier.Text}'" + Environment.NewLine +
                        $"File: {this.Program.GetSyntaxTree().FilePath}" + Environment.NewLine +
                        $"Lines: {statesSeen[decl.Identifier.Text].Identifier.TextUnit.Line} and {decl.Identifier.TextUnit.Line}");
                }
                else
                {
                    statesSeen.Add(decl.Identifier.Text, decl);
                }
            }

            var groupsSeen = new Dictionary<string, StateGroupDeclaration>();
            foreach (var decl in this.StateGroupDeclarations)
            {
                if (groupsSeen.ContainsKey(decl.Identifier.Text))
                {
                    throw new RewritingException(
                        $"Multiple declarations of the state group '{decl.Identifier.Text}'" + Environment.NewLine +
                        $"File: {this.Program.GetSyntaxTree().FilePath}" + Environment.NewLine +
                        $"Lines: {groupsSeen[decl.Identifier.Text].Identifier.TextUnit.Line} and {decl.Identifier.TextUnit.Line}");
                }
                else
                {
                    groupsSeen.Add(decl.Identifier.Text, decl);
                }
            }

            this.StateGroupDeclarations.ForEach(g => g.CheckDeclaration());
        }

        /// <summary>
        /// Returns the rewritten state group declaration.
        /// </summary>
        private string GetRewrittenStateGroupDeclaration(int indentLevel)
        {
            var indent = GetIndent(indentLevel);
            string text = indent;

            if (this.Group != null)
            {
                // When inside a group, the state should be made public.
                text += "public ";
            }
            else
            {
                // Otherwise, we look at the access modifier provided by the user.
                if (this.AccessModifier == AccessModifier.Protected)
                {
                    text += "protected ";
                }
                else if (this.AccessModifier == AccessModifier.Private)
                {
                    text += "private ";
                }
            }

            text += "class " + this.Identifier.TextUnit.Text + " : StateGroup";
            text += "\n" + indent + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            var newLine = string.Empty;  // no newline for the first
            foreach (var node in this.StateGroupDeclarations)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            foreach (var node in this.StateDeclarations)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            return text;
        }
    }
}
