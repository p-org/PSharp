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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Namespace declaration node.
    /// </summary>
    public sealed class NamespaceDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The namespace keyword.
        /// </summary>
        public Token NamespaceKeyword;

        /// <summary>
        /// The identifier tokens.
        /// </summary>
        public List<Token> IdentifierTokens;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        public Token LeftCurlyBracketToken;

        /// <summary>
        /// List of event declarations.
        /// </summary>
        public List<EventDeclarationNode> EventDeclarations;

        /// <summary>
        /// List of machine declarations.
        /// </summary>
        public List<MachineDeclarationNode> MachineDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        public Token RightCurlyBracketToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public NamespaceDeclarationNode()
        {
            this.IdentifierTokens = new List<Token>();
            this.EventDeclarations = new List<EventDeclarationNode>();
            this.MachineDeclarations = new List<MachineDeclarationNode>();
        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal override void Rewrite(ref int position)
        {
            var start = position;

            foreach (var node in this.EventDeclarations)
            {
                node.Rewrite(ref position);
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.Rewrite(ref position);
            }

            var text = this.NamespaceKeyword.TextUnit.Text;
            base.RewrittenTokens.Add(this.NamespaceKeyword);
            text += " ";

            foreach (var token in this.IdentifierTokens)
            {
                base.RewrittenTokens.Add(token);
                text += token.TextUnit.Text;
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.LeftCurlyBracketToken);

            foreach (var node in this.EventDeclarations)
            {
                text += node.GetRewrittenText();
                base.RewrittenTokens.AddRange(node.RewrittenTokens);
            }

            foreach (var node in this.MachineDeclarations)
            {
                text += node.GetRewrittenText();
                base.RewrittenTokens.AddRange(node.RewrittenTokens);
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.RightCurlyBracketToken);

            base.RewrittenTextUnit = new TextUnit(text, text.Length, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            foreach (var node in this.EventDeclarations)
            {
                node.GenerateTextUnit();
            }

            foreach (var node in this.MachineDeclarations)
            {
                node.GenerateTextUnit();
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
                text += node.GetFullText();
            }

            foreach (var node in this.MachineDeclarations)
            {
                text += node.GetFullText();
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            int length = this.RightCurlyBracketToken.TextUnit.End - this.NamespaceKeyword.TextUnit.Start + 1;

            base.TextUnit = new TextUnit(text, length, this.NamespaceKeyword.TextUnit.Start);
        }

        #endregion
    }
}
