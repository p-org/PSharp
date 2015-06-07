//-----------------------------------------------------------------------
// <copyright file="SendStatementNode.cs">
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
    /// Send statement node.
    /// </summary>
    internal sealed class SendStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The send keyword.
        /// </summary>
        internal Token SendKeyword;

        /// <summary>
        /// The machine identifier.
        /// </summary>
        internal ExpressionNode MachineIdentifier;
        
        /// <summary>
        /// The machine separator token.
        /// </summary>
        internal Token MachineSeparator;

        /// <summary>
        /// The event identifier.
        /// </summary>
        internal Token EventIdentifier;

        /// <summary>
        /// The event separator token.
        /// </summary>
        internal Token EventSeparator;

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
        internal SendStatementNode(IPSharpProgram program, StatementBlockNode node)
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
            this.MachineIdentifier.Rewrite();

            if (this.Payload != null)
            {
                this.Payload.Rewrite();
            }

            var text = this.GetRewrittenSendStatement();

            base.TextUnit = new TextUnit(text, this.SendKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.MachineIdentifier.Model();

            if (this.Payload != null)
            {
                this.Payload.Model();
            }

            var text = this.GetRewrittenSendStatement();

            base.TextUnit = new TextUnit(text, this.SendKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten send statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenSendStatement()
        {
            var text = "this.Send(";

            text += this.MachineIdentifier.TextUnit.Text;

            text += ", new ";

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

            if (this.Payload != null)
            {
                text += this.Payload.TextUnit.Text;
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
