//-----------------------------------------------------------------------
// <copyright file="PMonitorStatementNode.cs">
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
    /// Monitor statement node.
    /// </summary>
    internal sealed class PMonitorStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The monitor keyword.
        /// </summary>
        internal Token MonitorKeyword;

        /// <summary>
        /// The event identifier.
        /// </summary>
        internal Token EventIdentifier;

        /// <summary>
        /// The monitor comma token.
        /// </summary>
        internal Token MonitorComma;

        /// <summary>
        /// The monitor identifier.
        /// </summary>
        internal Token MonitorIdentifier;

        /// <summary>
        /// The event comma token.
        /// </summary>
        internal Token EventComma;

        /// <summary>
        /// The event payload.
        /// </summary>
        internal PExpressionNode Payload;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        internal PMonitorStatementNode(StatementBlockNode node)
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
            var text = "this.Monitor<";

            text += this.MonitorIdentifier.TextUnit.Text;

            text += ">(new ";

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

            base.TextUnit = new TextUnit(text, this.MonitorKeyword.TextUnit.Line);
        }

        #endregion
    }
}
