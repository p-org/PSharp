//-----------------------------------------------------------------------
// <copyright file="SendStatementNode.cs">
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
    /// Send statement node.
    /// </summary>
    public sealed class SendStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The send keyword.
        /// </summary>
        public Token SendKeyword;

        /// <summary>
        /// The event identifier.
        /// </summary>
        public Token EventIdentifier;

        /// <summary>
        /// The to keyword.
        /// </summary>
        public Token ToKeyword;

        /// <summary>
        /// The machine identifier.
        /// </summary>
        public ExpressionNode MachineIdentifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        public Token LeftParenthesisToken;

        /// <summary>
        /// The event payload.
        /// </summary>
        public ExpressionNode Payload;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        public Token RightParenthesisToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        public SendStatementNode(StatementBlockNode node)
            : base(node)
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

            var text = "this.Send(";

            this.MachineIdentifier.Rewrite(ref position);
            text += this.MachineIdentifier.GetRewrittenText();

            text += ", new " + this.EventIdentifier.TextUnit.Text + "(";

            if (this.Payload != null)
            {
                this.Payload.Rewrite(ref position);
                text += this.Payload.GetRewrittenText();
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.RewrittenTextUnit = new TextUnit(text, this.SendKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.SendKeyword.TextUnit.Text;
            text += " ";

            text += this.EventIdentifier.TextUnit.Text;
            text += " ";

            if (this.LeftParenthesisToken != null)
            {
                this.Payload.GenerateTextUnit();
                text += this.LeftParenthesisToken.TextUnit.Text;
                text += this.Payload.GetFullText();
                text += this.RightParenthesisToken.TextUnit.Text;
            }

            text += this.ToKeyword.TextUnit.Text;
            text += " ";

            this.MachineIdentifier.GenerateTextUnit();
            text += this.MachineIdentifier.GetFullText();

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.SendKeyword.TextUnit.Line,
                this.SendKeyword.TextUnit.Start);
        }

        #endregion
    }
}
