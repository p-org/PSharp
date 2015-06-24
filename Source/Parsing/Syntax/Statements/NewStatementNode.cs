//-----------------------------------------------------------------------
// <copyright file="NewStatementNode.cs">
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
    /// New statement node.
    /// </summary>
    internal sealed class NewStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The new keyword.
        /// </summary>
        internal Token NewKeyword;

        /// <summary>
        /// The type identifier.
        /// </summary>
        internal Token TypeIdentifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The constructor arguments.
        /// </summary>
        internal ExpressionNode Arguments;

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
        internal NewStatementNode(IPSharpProgram program, BlockSyntax node)
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
            if (this.Arguments != null)
            {
                this.Arguments.Rewrite();
            }

            var text = this.GetRewrittenNewStatement();

            base.TextUnit = new TextUnit(text, this.NewKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            if (this.Arguments != null)
            {
                this.Arguments.Model();
            }

            var text = this.GetRewrittenNewStatement();

            base.TextUnit = new TextUnit(text, this.NewKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten new statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenNewStatement()
        {
            var text = this.NewKeyword.TextUnit.Text;
            text += " ";

            text += this.TypeIdentifier.TextUnit.Text;

            text += "(";

            if (this.Arguments != null)
            {
                text += this.Arguments.TextUnit.Text;
            }

            text += ")";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
