//-----------------------------------------------------------------------
// <copyright file="CreateStatementNode.cs">
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
    /// Create statement node.
    /// </summary>
    public sealed class CreateStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The create keyword.
        /// </summary>
        public Token CreateKeyword;

        /// <summary>
        /// The machine identifier.
        /// </summary>
        public List<Token> MachineIdentifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        public Token LeftParenthesisToken;

        /// <summary>
        /// The machine creation payload.
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
        public CreateStatementNode(StatementBlockNode node)
            : base(node)
        {
            this.MachineIdentifier = new List<Token>();
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

            var text = "this.Create<";

            foreach (var id in this.MachineIdentifier)
            {
                text += id.TextUnit.Text;
            }

            text += ">(";

            this.Payload.Rewrite(ref position);
            text += this.Payload.GetRewrittenText();

            text += ")";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.RewrittenTextUnit = new TextUnit(text, this.CreateKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.CreateKeyword.TextUnit.Text;
            text += " ";

            foreach (var id in this.MachineIdentifier)
            {
                text += id.TextUnit.Text;
            }

            if (this.LeftParenthesisToken != null &&
                this.RightParenthesisToken != null)
            {
                text += this.LeftParenthesisToken.TextUnit.Text;
            }

            this.Payload.GenerateTextUnit();
            text += this.Payload.GetFullText();

            if (this.LeftParenthesisToken != null &&
                this.RightParenthesisToken != null)
            {
                text += this.RightParenthesisToken.TextUnit.Text;
            }
            
            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.CreateKeyword.TextUnit.Line,
                this.CreateKeyword.TextUnit.Start);
        }

        #endregion
    }
}
