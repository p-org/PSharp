//-----------------------------------------------------------------------
// <copyright file="StateGroupDeclaration.cs">
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
    /// State declaration syntax node.
    /// </summary>
    internal sealed class StateGroupDeclaration : PSharpSyntaxNode
    {
        #region fields

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

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="groupNode">StateGroupDeclaration</param>
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
        internal override void Rewrite()
        {
            string text = "";

            foreach (var node in this.StateGroupDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.StateDeclarations)
            {
                node.Rewrite();
            }

            try
            {
                text = this.GetRewrittenStateGroupDeclaration();
            }
            catch (Exception ex)
            {
                IO.Debug("Exception was thrown during rewriting:");
                IO.Debug(ex.Message);
                IO.Debug(ex.StackTrace);
                ErrorReporter.ReportAndExit("Failed to rewrite state group '{0}' of machine '{1}'.",
                    this.Identifier.TextUnit.Text, this.Machine.Identifier.TextUnit.Text);
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.StateGroupKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten state group declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenStateGroupDeclaration()
        {
            string text = "";
            
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
            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.StateGroupDeclarations)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.TextUnit.Text;
            }

            return text;
        }

        #endregion
    }
}
