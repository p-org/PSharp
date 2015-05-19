//-----------------------------------------------------------------------
// <copyright file="IfStatementNode.cs">
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
    /// If statement node.
    /// </summary>
    public sealed class IfStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The if keyword.
        /// </summary>
        public Token IfKeyword;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        public Token LeftParenthesisToken;

        /// <summary>
        /// The guard predicate.
        /// </summary>
        public ExpressionNode Guard;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        public Token RightParenthesisToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        public StatementBlockNode StatementBlock;

        /// <summary>
        /// The else keyword.
        /// </summary>
        public Token ElseKeyword;

        /// <summary>
        /// The else statement block.
        /// </summary>
        public StatementBlockNode ElseStatementBlock;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        public IfStatementNode(StatementBlockNode node)
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
            
            var text = "";

            text += this.IfKeyword.TextUnit.Text + " ";

            text += this.LeftParenthesisToken.TextUnit.Text;

            this.Guard.Rewrite(ref position);
            text += this.Guard.GetRewrittenText();

            text += this.RightParenthesisToken.TextUnit.Text;

            this.StatementBlock.Rewrite(ref position);
            text += this.StatementBlock.GetRewrittenText();

            if (this.ElseKeyword != null)
            {
                text += this.ElseKeyword.TextUnit.Text + " ";
                if (this.ElseStatementBlock != null)
                {
                    this.ElseStatementBlock.Rewrite(ref position);
                    text += this.ElseStatementBlock.GetRewrittenText();
                }
            }

            base.RewrittenTextUnit = new TextUnit(text, this.IfKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            this.Guard.GenerateTextUnit();
            this.StatementBlock.GenerateTextUnit();

            var text = "";

            text += this.IfKeyword.TextUnit.Text + " ";

            text += this.LeftParenthesisToken.TextUnit.Text;

            text += this.Guard.GetFullText();

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.StatementBlock.GetFullText();

            if (this.ElseKeyword != null)
            {
                text += this.ElseKeyword.TextUnit.Text + " ";
                if (this.ElseStatementBlock != null)
                {
                    this.ElseStatementBlock.GenerateTextUnit();
                    text += this.ElseStatementBlock.GetFullText();
                }
            }

            base.TextUnit = new TextUnit(text, this.IfKeyword.TextUnit.Line,
                this.IfKeyword.TextUnit.Start);
        }

        #endregion
    }
}
