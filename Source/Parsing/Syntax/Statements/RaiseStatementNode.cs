//-----------------------------------------------------------------------
// <copyright file="RaiseStatementNode.cs">
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
    /// Raise statement node.
    /// </summary>
    internal sealed class RaiseStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The raise keyword.
        /// </summary>
        internal Token RaiseKeyword;

        /// <summary>
        /// The event identifier.
        /// </summary>
        internal Token EventIdentifier;

        /// <summary>
        /// The separator token.
        /// </summary>
        internal Token Separator;

        /// <summary>
        /// The event payload.
        /// </summary>
        internal ExpressionNode Payload;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal RaiseStatementNode(IPSharpProgram program, StatementBlockNode node)
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
            if (this.Separator != null)
            {
                this.Payload.Rewrite();
            }

            var text = this.GetRewrittenRaiseStatement();

            base.TextUnit = new TextUnit(text, this.RaiseKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            if (this.Separator != null)
            {
                this.Payload.Model();
            }

            var text = this.GetRewrittenRaiseStatement();

            base.TextUnit = new TextUnit(text, this.RaiseKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten raise statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenRaiseStatement()
        {
            var text = "{\n";
            text += "this.Raise(new ";

            if (this.EventIdentifier.Type == TokenType.HaltEvent)
            {
                text += "Microsoft.PSharp.Halt";
            }
            else if (this.EventIdentifier.Type == TokenType.DefaultEvent)
            {
                text += "Microsoft.PSharp.Default";
            }
            else
            {
                text += this.EventIdentifier.TextUnit.Text;
            }

            text += "(";

            if (this.Separator != null)
            {
                text += this.Payload.TextUnit.Text;
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            text += "return;\n";
            text += "}\n";

            return text;
        }

        #endregion
    }
}
