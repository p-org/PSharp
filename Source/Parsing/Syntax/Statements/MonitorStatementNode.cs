//-----------------------------------------------------------------------
// <copyright file="MonitorStatementNode.cs">
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
    internal sealed class MonitorStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The monitor keyword.
        /// </summary>
        internal Token MonitorKeyword;

        /// <summary>
        /// The monitor identifier.
        /// </summary>
        internal ExpressionNode MonitorIdentifier;

        /// <summary>
        /// The monitor separator token.
        /// </summary>
        internal Token MonitorSeparator;

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
        internal MonitorStatementNode(IPSharpProgram program, BlockSyntax node)
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
            base.TextUnit = new TextUnit("", 0);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.MonitorIdentifier.Model();

            if (this.Payload != null)
            {
                this.Payload.Model();
            }

            var text = this.GetRewrittenMonitorStatement();

            base.TextUnit = new TextUnit(text, this.MonitorKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten monitor statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenMonitorStatement()
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
                text += this.Payload.TextUnit.Text;
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
