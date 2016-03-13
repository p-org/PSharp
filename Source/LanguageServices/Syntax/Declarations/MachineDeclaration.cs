//-----------------------------------------------------------------------
// <copyright file="MachineDeclaration.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
        /// List of method declarations.
        /// </summary>
        internal List<MethodDeclaration> MethodDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

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
            this.MethodDeclarations = new List<MethodDeclaration>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
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
            
            return text;
        }

        /// <summary>
        /// Returns the rewritten state on entry and on exit actions.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenStateOnEntryAndExitActions()
        {
            string text = "";

            foreach (var state in this.StateDeclarations)
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

            foreach (var state in this.StateDeclarations)
            {
                foreach (var withAction in state.TransitionsOnExitActions)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "protected void psharp_" + state.Identifier.TextUnit.Text + "_" +
                        withAction.Key.TextUnit.Text + "_action()";
                    text += onExitAction.TextUnit.Text + "\n";
                }
            }

            foreach (var state in this.StateDeclarations)
            {
                foreach (var withAction in state.ActionHandlers)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "protected void psharp_" + state.Identifier.TextUnit.Text + "_" +
                        withAction.Key.TextUnit.Text + "_action()";
                    text += onExitAction.TextUnit.Text + "\n";
                }
            }

            return text;
        }

        #endregion
    }
}
