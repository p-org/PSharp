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
        /// The event identifier.
        /// </summary>
        internal Token EventIdentifier;

        /// <summary>
        /// The machine separator token.
        /// </summary>
        internal Token MachineSeparator;

        /// <summary>
        /// The machine identifier.
        /// </summary>
        internal ExpressionNode MachineIdentifier;

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
        /// <param name="node">Node</param>
        internal SendStatementNode(StatementBlockNode node)
            : base(node)
        {

        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            var text = "this.Send(";

            this.MachineIdentifier.Rewrite(program);
            text += this.MachineIdentifier.GetRewrittenText();

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
                this.Payload.Rewrite(program);
                text += this.Payload.GetRewrittenText();
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.SendKeyword.TextUnit.Line);
        }

        #endregion
    }
}
