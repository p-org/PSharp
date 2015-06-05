//-----------------------------------------------------------------------
// <copyright file="ForeachStatementNode.cs">
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
    /// Foreach statement node.
    /// </summary>
    internal sealed class ForeachStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The foreach keyword.
        /// </summary>
        internal Token ForeachKeyword;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The guard predicate.
        /// </summary>
        internal ExpressionNode Guard;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal StatementBlockNode StatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        internal ForeachStatementNode(StatementBlockNode node)
            : base(node)
        {

        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            var text = "";

            text += this.ForeachKeyword.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            this.Guard.Rewrite(program);
            text += this.Guard.GetRewrittenText();

            text += this.RightParenthesisToken.TextUnit.Text;

            this.StatementBlock.Rewrite(program);
            text += this.StatementBlock.GetRewrittenText();

            base.RewrittenTextUnit = new TextUnit(text, this.ForeachKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            this.Guard.GenerateTextUnit();
            this.StatementBlock.GenerateTextUnit();

            var text = "";

            text += this.ForeachKeyword.TextUnit.Text;
            text += " ";

            text += this.LeftParenthesisToken.TextUnit.Text;

            text += this.Guard.GetFullText();

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.StatementBlock.GetFullText();

            base.TextUnit = new TextUnit(text, this.ForeachKeyword.TextUnit.Line);
        }

        #endregion
    }
}
