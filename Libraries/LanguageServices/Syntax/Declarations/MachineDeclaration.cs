//-----------------------------------------------------------------------
// <copyright file="MachineDeclaration.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Machine declaration syntax node.
    /// </summary>
    internal sealed class MachineDeclaration : PSharpSyntaxNode
    {
        #region fields

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
        internal List<EventDeclaration> EventDeclarations;

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

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="namespaceNode">NamespaceDeclaration</param>
        /// <param name="isMonitor">Is a monitor</param>
        /// <param name="modSet">Modifier set</param>
        internal MachineDeclaration(IPSharpProgram program, NamespaceDeclaration namespaceNode,
            bool isMonitor, ModifierSet modSet)
            : base(program)
        {
            this.Namespace = namespaceNode;
            this.IsMonitor = isMonitor;
            this.AccessModifier = modSet.AccessModifier;
            this.InheritanceModifier = modSet.InheritanceModifier;
            this.IsPartial = modSet.IsPartial;
            this.BaseNameTokens = new List<Token>();
            this.EventDeclarations = new List<EventDeclaration>();
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
        internal override void Rewrite()
        {
            string text = "";

            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.FieldDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.StateDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.StateGroupDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.Rewrite();
            }

            try
            {
                text = this.GetRewrittenMachineDeclaration();
            }
            catch (Exception ex)
            {
                IO.Debug("Exception was thrown during rewriting:");
                IO.Debug(ex.Message);
                IO.Debug(ex.StackTrace);
                IO.Error.ReportAndExit("Failed to rewrite {0} '{1}'.",
                    this.IsMonitor ? "monitor" : "machine", this.Identifier.TextUnit.Text);
            }

            foreach (var node in this.MethodDeclarations)
            {
                text += node.TextUnit.Text;
            }

            text += this.GetRewrittenStateOnEntryAndExitActions();
            text += this.GetRewrittenWithActions();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.MachineKeyword.TextUnit.Line);

            this.PopulateRewrittenMethodsWithStates();
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
        /// Sanity checking:
        /// -- no duplicate states and groups
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
                        $"File: {Program.GetSyntaxTree().FilePath}" + Environment.NewLine +
                        $"Lines: {statesSeen[decl.Identifier.Text].Identifier.TextUnit.Line} and {decl.Identifier.TextUnit.Line}"
                        );
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
                        $"File: {Program.GetSyntaxTree().FilePath}" + Environment.NewLine +
                        $"Lines: {groupsSeen[decl.Identifier.Text].Identifier.TextUnit.Line} and {decl.Identifier.TextUnit.Line}"
                        );

                }
                else
                {
                    groupsSeen.Add(decl.Identifier.Text, decl);
                }
            }

            this.StateGroupDeclarations.ForEach(g => g.CheckDeclaration());
        }

        #endregion

        #region private methods


        /// <summary>
        /// Returns the rewritten machine declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenMachineDeclaration()
        {
            string text = "";

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

            text += "class " + this.Identifier.TextUnit.Text + " : ";

            if (this.ColonToken != null)
            {
                foreach (var node in this.BaseNameTokens)
                {
                    text += node.TextUnit.Text;
                }
            }
            else if (!this.IsMonitor)
            {
                text += "Machine";
            }
            else
            {
                text += "Monitor";
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.EventDeclarations)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in this.FieldDeclarations)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in this.StateGroupDeclarations)
            {
                text += node.TextUnit.Text;
            }

            return text;
        }

        /// <summary>
        /// Returns the rewritten state on entry and on exit actions.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenStateOnEntryAndExitActions()
        {
            string text = "";

            foreach (var state in this.GetAllStateDeclarations())
            {
                if (state.EntryDeclaration != null)
                {
                    state.EntryDeclaration.Rewrite();
                    text += state.EntryDeclaration.TextUnit.Text + "\n";
                }

                if (state.ExitDeclaration != null)
                {
                    state.ExitDeclaration.Rewrite();
                    text += state.ExitDeclaration.TextUnit.Text + "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// Returns the rewritten with actions.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenWithActions()
        {
            string text = "";

            foreach (var state in this.GetAllStateDeclarations())
            {
                foreach (var withAction in state.TransitionsOnExitActions)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "protected void psharp_" + state.GetFullyQualifiedName() +
                        state.GetResolvedEventHandlerName(withAction.Key) + "_action()";
                    text += onExitAction.TextUnit.Text + "\n";
                }
            }

            foreach (var state in this.GetAllStateDeclarations())
            {
                foreach (var withAction in state.ActionHandlers)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "protected void psharp_" + state.GetFullyQualifiedName() +
                        state.GetResolvedEventHandlerName(withAction.Key) + "_action()";
                    text += onExitAction.TextUnit.Text + "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// Populated the set of rewritten methods and
        /// the states they came from.
        /// </summary>
        private void PopulateRewrittenMethodsWithStates()
        {
            foreach (var state in this.GetAllStateDeclarations())
            {
                var tokens = new List<string>();
                tokens.Insert(0, state.Identifier.TextUnit.Text);

                var group = state.Group;
                while (group != null)
                {
                    tokens.Insert(0, group.Identifier.TextUnit.Text);
                    group = group.Group;
                }
                
                foreach (var method in state.RewrittenMethods)
                {
                    method.QualifiedStateName = tokens;
                    this.RewrittenMethods.Add(method);
                } 
            }
        }

        #endregion
    }
}
