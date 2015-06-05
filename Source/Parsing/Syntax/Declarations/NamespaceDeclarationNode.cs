//-----------------------------------------------------------------------
// <copyright file="NamespaceDeclarationNode.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Namespace declaration node.
    /// </summary>
    internal sealed class NamespaceDeclarationNode : PSharpSyntaxNode
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
        internal List<EventDeclarationNode> EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        internal List<MachineDeclarationNode> MachineDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal NamespaceDeclarationNode()
        {
            this.IdentifierTokens = new List<Token>();
            this.EventDeclarations = new List<EventDeclarationNode>();
            this.MachineDeclarations = new List<MachineDeclarationNode>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite(program);
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.Rewrite(program);
            }

            var text = this.NamespaceKeyword.TextUnit.Text;
            text += " ";

            foreach (var token in this.IdentifierTokens)
            {
                text += token.TextUnit.Text;
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.EventDeclarations)
            {
                text += node.GetRewrittenText();
            }

            foreach (var node in this.MachineDeclarations)
            {
                text += node.GetRewrittenText();
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.NamespaceKeyword.TextUnit.Line);
        }

        #endregion
    }
}
