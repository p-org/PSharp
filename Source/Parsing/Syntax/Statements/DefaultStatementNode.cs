//-----------------------------------------------------------------------
// <copyright file="DefaultStatementNode.cs">
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
    /// Default statement node.
    /// </summary>
    public sealed class DefaultStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The default keyword.
        /// </summary>
        public Token DefaultKeyword;
        
        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        public Token LeftParenthesisToken;

        /// <summary>
        /// The type node.
        /// </summary>
        public PTypeNode TypeNode;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        public Token RightParenthesisToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        public DefaultStatementNode(StatementBlockNode node)
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

            var text = this.DefaultKeyword.TextUnit.Text;

            text += "(";

            this.TypeNode.Rewrite(ref position);
            text += this.TypeNode.GetRewrittenText();

            text += ")";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.RewrittenTextUnit = new TextUnit(text, this.DefaultKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.DefaultKeyword.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            this.TypeNode.GenerateTextUnit();
            text += this.TypeNode.GetFullText();

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.DefaultKeyword.TextUnit.Line,
                this.DefaultKeyword.TextUnit.Start);
        }

        #endregion
    }
}
