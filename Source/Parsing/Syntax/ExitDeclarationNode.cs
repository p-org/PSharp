//-----------------------------------------------------------------------
// <copyright file="ExitDeclarationNode.cs">
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
    /// Exit declaration node.
    /// </summary>
    public sealed class ExitDeclarationNode : BaseActionDeclarationNode
    {
        #region fields

        /// <summary>
        /// The exit keyword.
        /// </summary>
        public Token ExitKeyword;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="stateNode">StateDeclarationNode</param>
        public ExitDeclarationNode(MachineDeclarationNode machineNode, StateDeclarationNode stateNode)
            : base(machineNode, stateNode)
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

            var protectedKeyword = "protected";
            var protectedTextUnit = new TextUnit(protectedKeyword, protectedKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(protectedTextUnit, this.ExitKeyword.Line, TokenType.Protected));
            text += protectedKeyword;
            text += " ";

            var overrideKeyword = "override";
            var overrideTextUnit = new TextUnit(overrideKeyword, overrideKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(overrideTextUnit, this.ExitKeyword.Line, TokenType.Override));
            text += overrideKeyword;
            text += " ";

            var voidKeyword = "void";
            var voidTextUnit = new TextUnit(voidKeyword, voidKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(voidTextUnit, this.ExitKeyword.Line, TokenType.TypeIdentifier));
            text += voidKeyword;
            text += " ";

            var onExitKeyword = "OnExit";
            var onExitTextUnit = new TextUnit(onExitKeyword, onExitKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(onExitTextUnit, this.ExitKeyword.Line, TokenType.Identifier));
            text += onExitKeyword;

            var leftParenthesis = "(";
            var leftParenthesisTextUnit = new TextUnit(leftParenthesis, leftParenthesis.Length, text.Length);
            base.RewrittenTokens.Add(new Token(leftParenthesisTextUnit, this.ExitKeyword.Line, TokenType.LeftParenthesis));
            text += leftParenthesis;

            var rightParenthesis = ")";
            var rightParenthesisTextUnit = new TextUnit(rightParenthesis, rightParenthesis.Length, text.Length);
            base.RewrittenTokens.Add(new Token(rightParenthesisTextUnit, this.ExitKeyword.Line, TokenType.RightParenthesis));
            text += rightParenthesis;

            text += "\n" + base.LeftCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.LeftCurlyBracketToken);

            foreach (var stmt in base.RewriteStatements())
            {
                text += stmt.Text;//.TextUnit.Text;
                base.RewrittenTokens.Add(stmt);
            }

            text += base.RightCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.RightCurlyBracketToken);

            base.RewrittenTextUnit = new TextUnit(text, text.Length, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.ExitKeyword.TextUnit.Text;

            text += "\n" + base.LeftCurlyBracketToken.TextUnit.Text + "\n";

            text += base.RightCurlyBracketToken.TextUnit.Text + "\n";

            int length = base.RightCurlyBracketToken.TextUnit.End - this.ExitKeyword.TextUnit.Start + 1;

            base.TextUnit = new TextUnit(text, length, this.ExitKeyword.TextUnit.Start);
        }

        #endregion
    }
}
