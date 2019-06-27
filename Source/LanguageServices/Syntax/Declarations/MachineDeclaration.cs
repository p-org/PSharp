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
    /// Machine declaration syntax node.
    /// </summary>
    internal sealed class MachineDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The namespace parent node.
        /// </summary>
        internal readonly NamespaceDeclaration Namespace;

        /// <summary>
        /// True if the machine is a monitor.
        /// </summary>
        internal readonly bool IsMonitor;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal readonly AccessModifier AccessModifier;

        /// <summary>
        /// The inheritance modifier.
        /// </summary>
        internal readonly InheritanceModifier InheritanceModifier;

        /// <summary>
        /// True if the machine is partial.
        /// </summary>
        internal readonly bool IsPartial;

        /// <summary>
        /// The machine keyword.
        /// </summary>
        internal Token MachineKeyword;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The template parameters
        /// </summary>
        internal List<Token> TemplateParameters;

        /// <summary>
        /// The colon token.
        /// </summary>
        internal Token ColonToken;

        /// <summary>
        /// The base name tokens.
        /// </summary>
        internal List<Token> BaseNameTokens;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// List of event declarations.
        /// </summary>
        internal EventDeclarations EventDeclarations;

        /// <summary>
        /// List of field declarations.
        /// </summary>
        internal List<FieldDeclaration> FieldDeclarations;

        /// <summary>
        /// List of state declarations.
        /// </summary>
        internal List<StateDeclaration> StateDeclarations;

        /// <summary>
        /// List of state group declarations.
        /// </summary>
        internal List<StateGroupDeclaration> StateGroupDeclarations;

        /// <summary>
        /// List of method declarations.
        /// </summary>
        internal List<MethodDeclaration> MethodDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        /// <summary>
        /// Set of all rewritten method.
        /// </summary>
        internal HashSet<QualifiedMethod> RewrittenMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineDeclaration"/> class.
        /// </summary>
        internal MachineDeclaration(IPSharpProgram program, NamespaceDeclaration namespaceNode,
            bool isMonitor, ModifierSet modSet)
            : base(program)
        {
            this.Namespace = namespaceNode;
            this.IsMonitor = isMonitor;
            this.AccessModifier = modSet.AccessModifier;
            this.InheritanceModifier = modSet.InheritanceModifier;
            this.IsPartial = modSet.IsPartial;
            this.TemplateParameters = new List<Token>();
            this.BaseNameTokens = new List<Token>();
            this.EventDeclarations = new EventDeclarations();
            this.FieldDeclarations = new List<FieldDeclaration>();
            this.StateDeclarations = new List<StateDeclaration>();
            this.StateGroupDeclarations = new List<StateGroupDeclaration>();
            this.MethodDeclarations = new List<MethodDeclaration>();
            this.RewrittenMethods = new HashSet<QualifiedMethod>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            foreach (var node in this.EventDeclarations)
            {
                this.ProjectionNode.AddChild(node.ProjectionNode);
                node.Rewrite(indentLevel + 1);
            }

            foreach (var node in this.FieldDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }

            foreach (var node in this.StateDeclarations)
            {
                this.ProjectionNode.AddChild(node.ProjectionNode);
                node.Rewrite(indentLevel + 1);
            }

            foreach (var node in this.StateGroupDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.Rewrite(indentLevel + 1);
            }

            string text = string.Empty;
            string newLine = string.Empty;

            try
            {
                text = this.GetRewrittenMachineDeclaration(indentLevel, ref newLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit("Failed to rewrite {0} '{1}'.", this.IsMonitor ? "monitor" : "machine", this.Identifier.TextUnit.Text);
            }

            var indent = GetIndent(indentLevel);

            foreach (var node in this.MethodDeclarations)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            text += this.GetRewrittenStateOnEntryAndExitActions(indentLevel + 1, ref newLine, text.Length);
            text += this.GetRewrittenWithActions(indentLevel + 1, ref newLine);

            text += $"{indent}{this.RightCurlyBracketToken.TextUnit.Text}\n";

            this.TextUnit = this.MachineKeyword.TextUnit.WithText(text);

            // ProjectionNode is updated as part of TypeofRewriter actions on this
            this.PopulateRewrittenMethodsWithStateQualifiedNames();
        }

        /// <summary>
        /// Returns all state declarations inside this machine (recursively).
        /// </summary>
        internal List<StateDeclaration> GetAllStateDeclarations()
        {
            var decls = new List<StateDeclaration>();
            decls.AddRange(this.StateDeclarations);
            this.StateGroupDeclarations.ForEach(g => decls.AddRange(g.GetAllStateDeclarations()));
            return decls;
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
        /// Returns the rewritten machine declaration.
        /// </summary>
        private string GetRewrittenMachineDeclaration(int indentLevel, ref string newLine)
        {
            var indent = GetIndent(indentLevel);
            string text = indent;

            if (this.AccessModifier == AccessModifier.Public)
            {
                text += "public ";
            }
            else if (this.AccessModifier == AccessModifier.Internal)
            {
                text += "internal ";
            }

            if (this.IsPartial)
            {
                text += "partial ";
            }

            if (this.InheritanceModifier == InheritanceModifier.Abstract)
            {
                text += "abstract ";
            }

            text += $"class {this.Identifier.TextUnit.Text}";

            foreach (var node in this.TemplateParameters)
            {
                text += node.TextUnit.Text;
            }

            text += " : ";

            if (this.ColonToken != null)
            {
                foreach (var node in this.BaseNameTokens)
                {
                    text += node.TextUnit.Text;
                }
            }
            else
            {
                text += this.IsMonitor ? "Monitor" : "Machine";
            }

            this.ProjectionNode.SetHeaderInfo(this.HeaderTokenRange, indent.Length, text);

            text += $"\n{indent}{this.LeftCurlyBracketToken.TextUnit.Text}\n";

            foreach (var node in this.EventDeclarations)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            if (this.FieldDeclarations.Count > 0)
            {
                text += newLine;
                newLine = string.Empty;
            }

            foreach (var node in this.FieldDeclarations)
            {
                text += node.TextUnit.Text;
                newLine = "\n";     // Not added per-line
            }

            if (this.StateDeclarations.Count > 0)
            {
                text += newLine;
                newLine = string.Empty;
            }

            foreach (var node in this.StateDeclarations)
            {
                text += newLine;
                node.ProjectionNode.SetOffsetInParent(text.Length);
                text += node.TextUnit.Text;
                newLine = "\n";
            }

            if (this.StateGroupDeclarations.Count > 0)
            {
                text += newLine;
                newLine = string.Empty;
            }

            foreach (var node in this.StateGroupDeclarations)
            {
                text += newLine + node.TextUnit.Text;
                newLine = "\n";
            }

            return text;
        }

        /// <summary>
        /// Returns the rewritten state on-entry and on-exit actions.
        /// </summary>
        private string GetRewrittenStateOnEntryAndExitActions(int indentLevel, ref string newLine, int offset)
        {
            string text = string.Empty;
            foreach (var state in this.GetAllStateDeclarations())
            {
                this.ProcessEntryOrExitDeclaration(state, state.EntryDeclaration, ref text, indentLevel, ref newLine, offset);
                this.ProcessEntryOrExitDeclaration(state, state.ExitDeclaration, ref text, indentLevel, ref newLine, offset);
            }

            return text;
        }

        private void ProcessEntryOrExitDeclaration<T>(StateDeclaration state, T declaration, ref string text, int indentLevel,
                                                      ref string newLine, int offset)
                                                      where T : PSharpSyntaxNode
        {
            // This should be a local function but can't because of the ref parameter
            if (declaration != null)
            {
                // For the C# rewriting here, the parent is actually the machine instance
                // on which the rewritten method resides.
                state.ProjectionNode.AddChild(declaration.ProjectionNode, this.ProjectionNode);
                declaration.Rewrite(indentLevel);
                text += newLine;

                // The rewritten offset is relative to the machine, which is the C# parent.
                declaration.ProjectionNode.SetOffsetInParent(offset + text.Length);
                text += declaration.TextUnit.Text;
                newLine = "\n";
            }
        }

        /// <summary>
        /// Returns the rewritten with-actions.
        /// </summary>
        private string GetRewrittenWithActions(int indentLevel, ref string newLine)
        {
            string text = string.Empty;
            var indent = GetIndent(indentLevel);

            foreach (var state in this.GetAllStateDeclarations())
            {
                foreach (var withAction in state.TransitionsOnExitActions)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.BlockSyntax.Rewrite(indentLevel);
                    var typeStr = onExitAction.IsAsync ? "async System.Threading.Tasks.Task" : "void";
                    var suffix = onExitAction.IsAsync ? "_async()" : "()";
                    text += newLine + indent + $"protected {typeStr} psharp_" + state.GetFullyQualifiedName() +
                        state.GetResolvedEventHandlerName(withAction.Key) + $"_action{suffix}";
                    text += "\n" + onExitAction.BlockSyntax.TextUnit.Text + "\n";
                    newLine = "\n";
                }
            }

            foreach (var state in this.GetAllStateDeclarations())
            {
                foreach (var withAction in state.ActionHandlers)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.BlockSyntax.Rewrite(indentLevel);
                    var typeStr = onExitAction.IsAsync ? "async System.Threading.Tasks.Task" : "void";
                    var suffix = onExitAction.IsAsync ? "_async()" : "()";
                    text += newLine + indent + $"protected {typeStr} psharp_" + state.GetFullyQualifiedName() +
                        state.GetResolvedEventHandlerName(withAction.Key) + $"_action{suffix}";
                    text += "\n" + onExitAction.BlockSyntax.TextUnit.Text + "\n";
                    newLine = "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// Populated the set of rewritten methods with the group.state-qualified name of
        /// the states they came from.
        /// </summary>
        private void PopulateRewrittenMethodsWithStateQualifiedNames()
        {
            foreach (var state in this.GetAllStateDeclarations())
            {
                // Qualify with the state name.
                var tokens = new List<string>();
                tokens.Insert(0, state.Identifier.TextUnit.Text);

                // Back up the state's group parents, forming the chain of group names in reverse.
                var group = state.Group;
                while (group != null)
                {
                    tokens.Insert(0, group.Identifier.TextUnit.Text);
                    group = group.Group;
                }

                // Qualify the name of each state in this group.
                foreach (var method in state.RewrittenMethods)
                {
                    method.QualifiedStateName = tokens;
                    this.RewrittenMethods.Add(method);
                }
            }
        }
    }
}
