//-----------------------------------------------------------------------
// <copyright file="ActionDeclarationNode.cs">
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
    /// Action declaration node.
    /// </summary>
    public sealed class ActionDeclarationNode : BaseActionDeclarationNode
    {
        #region fields

        /// <summary>
        /// The action keyword.
        /// </summary>
        public Token ActionKeyword;

        /// <summary>
        /// The modifier token.
        /// </summary>
        public Token Modifier;

        /// <summary>
        /// The inheritance modifier token.
        /// </summary>
        public Token InheritanceModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        public Token SemicolonToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineNode">MachineDeclarationNode</param>
        public ActionDeclarationNode(MachineDeclarationNode machineNode)
            : base(machineNode, null)
        {
            
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
            var text = "";

            if (this.Modifier != null)
            {
                text += this.Modifier.TextUnit.Text;
                base.RewrittenTokens.Add(this.Modifier);
                text += " ";
            }

            if (this.InheritanceModifier != null)
            {
                text += this.InheritanceModifier.TextUnit.Text;
                base.RewrittenTokens.Add(this.InheritanceModifier);
                text += " ";
            }

            var voidKeyword = "void";
            var voidTextUnit = new TextUnit(voidKeyword, voidKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(voidTextUnit, this.ActionKeyword.Line, TokenType.TypeIdentifier));
            text += voidKeyword;
            text += " ";

            text += this.Identifier.TextUnit.Text;
            base.RewrittenTokens.Add(this.Identifier);

            var leftParenthesis = "(";
            var leftParenthesisTextUnit = new TextUnit(leftParenthesis, leftParenthesis.Length, text.Length);
            base.RewrittenTokens.Add(new Token(leftParenthesisTextUnit, this.ActionKeyword.Line, TokenType.LeftParenthesis));
            text += leftParenthesis;

            var rightParenthesis = ")";
            var rightParenthesisTextUnit = new TextUnit(rightParenthesis, rightParenthesis.Length, text.Length);
            base.RewrittenTokens.Add(new Token(rightParenthesisTextUnit, this.ActionKeyword.Line, TokenType.RightParenthesis));
            text += rightParenthesis;

            if (base.LeftCurlyBracketToken != null)
            {
                text += "\n" + base.LeftCurlyBracketToken.TextUnit.Text + "\n";
                base.RewrittenTokens.Add(this.LeftCurlyBracketToken);

                foreach (var stmt in base.RewriteStatements())
                {
                    text += stmt.Text;//.TextUnit.Text;
                    base.RewrittenTokens.Add(stmt);
                }

                text += base.RightCurlyBracketToken.TextUnit.Text + "\n";
                base.RewrittenTokens.Add(this.RightCurlyBracketToken);
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";
                base.RewrittenTokens.Add(this.SemicolonToken);
            }

            base.RewrittenTextUnit = new TextUnit(text, text.Length, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = "";
            var initToken = this.ActionKeyword;

            if (this.InheritanceModifier != null)
            {
                initToken = this.InheritanceModifier;
                text += this.InheritanceModifier.TextUnit.Text;
                text += " ";
            }

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            text += this.ActionKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            if (base.LeftCurlyBracketToken != null)
            {
                text += "\n" + base.LeftCurlyBracketToken.TextUnit.Text + "\n";

                foreach (var stmt in base.Statements)
                {
                    text += stmt.TextUnit.Text;
                }

                text += base.RightCurlyBracketToken.TextUnit.Text + "\n";

                int length = base.RightCurlyBracketToken.TextUnit.End - initToken.TextUnit.Start + 1;

                base.TextUnit = new TextUnit(text, length, initToken.TextUnit.Start);
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";

                int length = this.SemicolonToken.TextUnit.End - initToken.TextUnit.Start + 1;

                base.TextUnit = new TextUnit(text, length, initToken.TextUnit.Start);
            }
        }

        #endregion
    }
}
