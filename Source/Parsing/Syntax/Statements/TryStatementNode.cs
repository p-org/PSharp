//-----------------------------------------------------------------------
// <copyright file="TryStatementNode.cs">
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
    /// Try statement node.
    /// </summary>
    internal sealed class TryStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The try keyword.
        /// </summary>
        internal Token TryKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal StatementBlockNode StatementBlock;

        /// <summary>
        /// The catch keywords.
        /// </summary>
        internal List<Token> CatchKeywords;

        /// <summary>
        /// The catch left parenthesis tokens.
        /// </summary>
        internal List<Token> CatchLeftParenthesisTokens;

        /// <summary>
        /// The catch expressions.
        /// </summary>
        internal List<ExpressionNode> CatchExpressions;

        /// <summary>
        /// The catch right parenthesis tokens.
        /// </summary>
        internal List<Token> CatchRightParenthesisTokens;

        /// <summary>
        /// The catch statement blocks.
        /// </summary>
        internal List<StatementBlockNode> CatchStatementBlocks;

        /// <summary>
        /// The finally keyword.
        /// </summary>
        internal Token FinallyKeyword;

        /// <summary>
        /// The finally statement block.
        /// </summary>
        internal StatementBlockNode FinallyStatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal TryStatementNode(IPSharpProgram program, StatementBlockNode node)
            : base(program, node)
        {
            this.CatchKeywords = new List<Token>();
            this.CatchStatementBlocks = new List<StatementBlockNode>();
            this.CatchLeftParenthesisTokens = new List<Token>();
            this.CatchExpressions = new List<ExpressionNode>();
            this.CatchRightParenthesisTokens = new List<Token>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            this.StatementBlock.Rewrite();

            for (int idx = 0; idx < this.CatchKeywords.Count; idx++)
            {
                if (this.CatchLeftParenthesisTokens[idx] != null)
                {
                    this.CatchExpressions[idx].Rewrite();
                }

                if (this.CatchKeywords[idx] != null &&
                    this.CatchStatementBlocks[idx] != null)
                {
                    this.CatchStatementBlocks[idx].Rewrite();
                }
            }

            if (this.FinallyKeyword != null &&
                this.FinallyStatementBlock != null)
            {
                this.FinallyStatementBlock.Rewrite();
            }

            var text = this.GetRewrittenTryStatement();

            base.TextUnit = new TextUnit(text, this.TryKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.StatementBlock.Model();

            for (int idx = 0; idx < this.CatchKeywords.Count; idx++)
            {
                if (this.CatchLeftParenthesisTokens[idx] != null)
                {
                    this.CatchExpressions[idx].Model();
                }

                if (this.CatchKeywords[idx] != null &&
                    this.CatchStatementBlocks[idx] != null)
                {
                    this.CatchStatementBlocks[idx].Model();
                }
            }

            if (this.FinallyKeyword != null &&
                this.FinallyStatementBlock != null)
            {
                this.FinallyStatementBlock.Model();
            }

            var text = this.GetRewrittenTryStatement();

            base.TextUnit = new TextUnit(text, this.TryKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten try statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenTryStatement()
        {
            var text = "";

            text += this.TryKeyword.TextUnit.Text + " ";

            text += this.StatementBlock.TextUnit.Text;

            for (int idx = 0; idx < this.CatchKeywords.Count; idx++)
            {
                if (this.CatchKeywords[idx] != null)
                {
                    text += this.CatchKeywords[idx].TextUnit.Text + " ";

                    text += this.CatchLeftParenthesisTokens[idx].TextUnit.Text;
                    text += this.CatchExpressions[idx].TextUnit.Text;
                    text += this.CatchRightParenthesisTokens[idx].TextUnit.Text;

                    if (this.CatchStatementBlocks[idx] != null)
                    {
                        text += this.CatchStatementBlocks[idx].TextUnit.Text;
                    }
                }
            }

            if (this.FinallyKeyword != null)
            {
                text += this.FinallyKeyword.TextUnit.Text + " ";
                if (this.FinallyStatementBlock != null)
                {
                    text += this.FinallyStatementBlock.TextUnit.Text;
                }
            }

            return text;
        }

        #endregion
    }
}
