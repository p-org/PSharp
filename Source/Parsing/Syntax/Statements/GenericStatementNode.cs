//-----------------------------------------------------------------------
// <copyright file="GenericStatementNode.cs">
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
    /// Generic statement node.
    /// </summary>
    internal sealed class GenericStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The expression node.
        /// </summary>
        internal ExpressionNode Expression;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal GenericStatementNode(IPSharpProgram program, StatementBlockNode node)
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
            this.Expression.Rewrite();

            var text = this.GetRewrittenGenericStatement();

            base.TextUnit = new TextUnit(text, this.Expression.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.Expression.Model();

            var text = this.GetRewrittenGenericStatement();

            base.TextUnit = new TextUnit(text, this.Expression.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten generic statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenGenericStatement()
        {
            var text = "";

            text += this.Expression.TextUnit.Text;

            if (this.SemicolonToken != null)
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";
            }

            return text;
        }

        #endregion
    }
}
