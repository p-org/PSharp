//-----------------------------------------------------------------------
// <copyright file="ForStatement.cs">
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
    /// For statement syntax node.
    /// </summary>
    internal sealed class ForStatement : Statement
    {
        #region fields

        /// <summary>
        /// The for keyword.
        /// </summary>
        internal Token ForKeyword;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The iterator.
        /// </summary>
        internal ExpressionNode Iterator;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal ForStatement(IPSharpProgram program, BlockSyntax node)
            : base(program, node)
        {

        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            this.Iterator.Rewrite();
            this.StatementBlock.Rewrite();

            var text = this.GetRewrittenWhileStatement();

            base.TextUnit = new TextUnit(text, this.ForKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.Iterator.Model();
            this.StatementBlock.Model();

            var text = this.GetRewrittenWhileStatement();

            base.TextUnit = new TextUnit(text, this.ForKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten while statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenWhileStatement()
        {
            var text = "";

            text += this.ForKeyword.TextUnit.Text + " ";

            text += this.LeftParenthesisToken.TextUnit.Text;

            text += this.Iterator.TextUnit.Text;

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.StatementBlock.TextUnit.Text;

            return text;
        }

        #endregion
    }
}
