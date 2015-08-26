//-----------------------------------------------------------------------
// <copyright file="NamespaceDeclaration.cs">
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
        internal List<EventDeclaration> EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        internal List<MachineDeclaration> MachineDeclarations;

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
        internal NamespaceDeclaration(IPSharpProgram program)
            : base(program, false)
        {
            this.IdentifierTokens = new List<Token>();
            this.EventDeclarations = new List<EventDeclaration>();
            this.MachineDeclarations = new List<MachineDeclaration>();
        }
        
        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite()
        {
            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite();
            }
            
            foreach (var node in this.MachineDeclarations)
            {
                node.Rewrite();
            }
            
            var text = this.GetRewrittenNamespaceDeclaration();

            var realMachines = this.MachineDeclarations.FindAll(m => !m.IsModel && !m.IsMonitor);

            foreach (var node in realMachines)
            {
                text += node.TextUnit.Text;
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.NamespaceKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            foreach (var node in this.EventDeclarations)
            {
                node.Model();
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.Model();
            }

            var text = this.GetRewrittenNamespaceDeclaration();

            var realMachines = this.MachineDeclarations.FindAll(m => !m.IsModel && !m.IsMonitor);
            var modelMachines = this.MachineDeclarations.FindAll(m => m.IsModel);
            var monitors = this.MachineDeclarations.FindAll(m => m.IsMonitor);

            foreach (var node in realMachines)
            {
                if (!modelMachines.Any(m => m.Identifier.TextUnit.Text.Equals(
                    node.Identifier.TextUnit.Text)))
                {
                    text += node.TextUnit.Text;
                }
            }

            foreach (var node in modelMachines)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in monitors)
            {
                text += node.TextUnit.Text;
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.NamespaceKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten namespace declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenNamespaceDeclaration()
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
                text += node.TextUnit.Text;
            }

            return text;
        }

        #endregion
    }
}
