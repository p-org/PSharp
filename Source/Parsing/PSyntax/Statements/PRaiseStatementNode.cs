//-----------------------------------------------------------------------
// <copyright file="PRaiseStatementNode.cs">
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
    /// Raise statement node.
    /// </summary>
    public sealed class PRaiseStatementNode : PStatementNode
    {
        #region fields

        /// <summary>
        /// The raise keyword.
        /// </summary>
        public Token RaiseKeyword;

        /// <summary>
        /// The event identifier.
        /// </summary>
        public Token EventIdentifier;

        /// <summary>
        /// The comma token.
        /// </summary>
        public Token Comma;

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
        public PRaiseStatementNode(PStatementBlockNode node)
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

            if (this.Comma != null)
            {
                this.Payload.Rewrite(ref position);
                text += this.Payload.GetRewrittenText();
            }

            text += "))";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            text += "return;\n";
            text += "}\n";

            base.RewrittenTextUnit = new TextUnit(text, this.RaiseKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.RaiseKeyword.TextUnit.Text;
            text += " ";

            text += this.EventIdentifier.TextUnit.Text;
            text += " ";

            if (this.Comma != null)
            {
                this.Payload.GenerateTextUnit();
                text += this.Comma.TextUnit.Text;
                text += this.Payload.GetFullText();
            }

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.RaiseKeyword.TextUnit.Line,
                this.RaiseKeyword.TextUnit.Start);
        }

        #endregion
    }
}
