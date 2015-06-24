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
    internal sealed class IfStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The if keyword.
        /// </summary>
        internal Token IfKeyword;

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
        internal BlockSyntax StatementBlock;

        /// <summary>
        /// The else keyword.
        /// </summary>
        internal Token ElseKeyword;

        /// <summary>
        /// The else statement block.
        /// </summary>
        internal BlockSyntax ElseStatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal IfStatementNode(IPSharpProgram program, BlockSyntax node)
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
            this.Guard.Rewrite();
            this.StatementBlock.Rewrite();

            if (this.ElseKeyword != null &&
                this.ElseStatementBlock != null)
            {
                this.ElseStatementBlock.Rewrite();
            }

            var text = this.GetRewrittenIfStatement();

            base.TextUnit = new TextUnit(text, this.IfKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.Guard.Model();
            this.StatementBlock.Model();

            if (this.ElseKeyword != null &&
                this.ElseStatementBlock != null)
            {
                this.ElseStatementBlock.Model();
            }

            var text = this.GetRewrittenIfStatement();

            base.TextUnit = new TextUnit(text, this.IfKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten if statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenIfStatement()
        {
            var text = "";

            text += this.IfKeyword.TextUnit.Text + " ";

            text += this.LeftParenthesisToken.TextUnit.Text;

            text += this.Guard.TextUnit.Text;

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.StatementBlock.TextUnit.Text;

            if (this.ElseKeyword != null)
            {
                text += this.ElseKeyword.TextUnit.Text + " ";
                if (this.ElseStatementBlock != null)
                {
                    text += this.ElseStatementBlock.TextUnit.Text;
                }
            }

            return text;
        }

        #endregion
    }
}
