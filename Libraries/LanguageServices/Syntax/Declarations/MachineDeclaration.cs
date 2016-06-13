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
        /// True if the machine is a monitor.
        /// </summary>
        internal readonly bool IsMonitor;

        /// <summary>
        /// True if the machine is partial.
        /// </summary>
        internal bool IsPartial;

        /// <summary>
        /// The machine keyword.
        /// </summary>
        internal Token MachineKeyword;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The inheritance modifier.
        /// </summary>
        internal InheritanceModifier InheritanceModifier;

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
        /// Map for all generated methods
        /// </summary>
        internal Dictionary<string, List<string>> GeneratedMethodToQualifiedStateName;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="isMonitor">Is a monitor</param>
        /// <param name="isPartial">Is partial</param>
        internal MachineDeclaration(IPSharpProgram program, bool isMonitor, bool isPartial)
            : base(program)
        {
            this.IsMonitor = isMonitor;
            this.IsPartial = isPartial;
            this.BaseNameTokens = new List<Token>();
            this.FieldDeclarations = new List<FieldDeclaration>();
            this.StateDeclarations = new List<StateDeclaration>();
            this.StateGroupDeclarations = new List<StateGroupDeclaration>();
            this.MethodDeclarations = new List<MethodDeclaration>();
            this.GeneratedMethodToQualifiedStateName = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite()
        {
            string text = "";

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
                ErrorReporter.ReportAndExit("Failed to rewrite {0} '{1}'.",
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

            GatherGeneratedMethods();
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
                    text += "protected void psharp_" + state.GetFullyQualifiedName() + "_" +
                        withAction.Key.TextUnit.Text + "_action()";
                    text += onExitAction.TextUnit.Text + "\n";
                }
            }

            foreach (var state in this.GetAllStateDeclarations())
            {
                foreach (var withAction in state.ActionHandlers)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "protected void psharp_" + state.GetFullyQualifiedName() + "_" +
                        withAction.Key.TextUnit.Text + "_action()";
                    text += onExitAction.TextUnit.Text + "\n";
                }
            }

            return text;
        }

        /// <summary>
        /// Populate the set of generated methods and the states they came from.
        /// </summary>
        /// <returns>Text</returns>
        private void GatherGeneratedMethods()
        {
            var GetStateTokenList = new Func<StateDeclaration, List<string>>(state =>
            {
                var ls = new List<string>();
                ls.Insert(0, state.Identifier.TextUnit.Text);
                var group = state.Group;
                while (group != null)
                {
                    ls.Insert(0, group.Identifier.TextUnit.Text);
                    group = group.Group;
                }
                return ls;
            });

            foreach (var state in this.GetAllStateDeclarations())
            {
                var tokens = GetStateTokenList(state);
                foreach (var method in state.GeneratedMethodNames)
                    GeneratedMethodToQualifiedStateName.Add(method, tokens);
            }
        }

        #endregion
    }
}
