//-----------------------------------------------------------------------
// <copyright file="PDefaultStatement.cs">
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
    /// Default statement syntax node.
    /// </summary>
    internal sealed class PDefaultStatement : Statement
    {
        #region fields

        /// <summary>
        /// The default keyword.
        /// </summary>
        internal Token DefaultKeyword;
        
        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The actual type.
        /// </summary>
        internal PBaseType Type;

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
        internal PDefaultStatement(IPSharpProgram program, BlockSyntax node)
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
            var text = this.GetRewrittenDefaultStatement();
            base.TextUnit = new TextUnit(text, this.DefaultKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenDefaultStatement();
            base.TextUnit = new TextUnit(text, this.DefaultKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten default statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenDefaultStatement()
        {
            var text = "";

            this.Type.Rewrite();
            if (this.Type.Type == PType.Seq ||
                this.Type.Type == PType.Map)
            {
                text += "new ";
                text += this.Type.GetRewrittenText();
                text += "()";
            }
            else
            {
                text += this.DefaultKeyword.TextUnit.Text;
                text += "(";
                text += this.Type.GetRewrittenText();
                text += ")";
            }

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
