//-----------------------------------------------------------------------
// <copyright file="AssertStatementNode.cs">
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
    /// Assert statement node.
    /// </summary>
    internal sealed class AssertStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The assert keyword.
        /// </summary>
        internal Token AssertKeyword;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The assert predicate.
        /// </summary>
        internal ExpressionNode Predicate;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal AssertStatementNode(IPSharpProgram program, StatementBlockNode node)
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
            this.Predicate.Rewrite();

            var text = this.GetRewrittenAssertStatement();

            base.TextUnit = new TextUnit(text, this.AssertKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.Predicate.Model();

            var text = this.GetRewrittenAssertStatement();

            base.TextUnit = new TextUnit(text, this.AssertKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten assert statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenAssertStatement()
        {
            var text = "this.Assert";

            text += this.LeftParenthesisToken.TextUnit.Text;

            text += this.Predicate.TextUnit.Text;

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
