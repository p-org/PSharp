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

namespace Microsoft.PSharp.Parsing.PSyntax
{
    /// <summary>
    /// Monitor statement node.
    /// </summary>
    public sealed class PMonitorStatementNode : PStatementNode
    {
        #region fields

        /// <summary>
        /// The monitor keyword.
        /// </summary>
        public Token MonitorKeyword;

        /// <summary>
        /// The event identifier.
        /// </summary>
        public Token EventIdentifier;

        /// <summary>
        /// The monitor comma token.
        /// </summary>
        public Token MonitorComma;

        /// <summary>
        /// The monitor identifier.
        /// </summary>
        public Token MonitorIdentifier;

        /// <summary>
        /// The event comma token.
        /// </summary>
        public Token EventComma;

        /// <summary>
        /// The event payload.
        /// </summary>
        public PExpressionNode Payload;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        public PMonitorStatementNode(PStatementBlockNode node)
            : base(node)
        {

        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal override void Rewrite(ref int position)
        {
            var start = position;

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
                this.Payload.Rewrite(ref position);
                text += this.Payload.GetRewrittenText();
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.RewrittenTextUnit = new TextUnit(text, this.MonitorKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.MonitorKeyword.TextUnit.Text;
            text += " ";

            text += this.MonitorIdentifier.TextUnit.Text;

            text += this.MonitorComma.TextUnit.Text;
            text += " ";

            text += this.EventIdentifier.TextUnit.Text;

            if (this.EventComma != null)
            {
                text += this.EventComma.TextUnit.Text;
                text += " ";

                this.Payload.GenerateTextUnit();
            }

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.MonitorKeyword.TextUnit.Line,
                this.MonitorKeyword.TextUnit.Start);
        }

        #endregion
    }
}
