//-----------------------------------------------------------------------
// <copyright file="PPayloadReceiveNode.cs">
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
    /// Payload receive node.
    /// </summary>
    public sealed class PPayloadReceiveNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The payload keyword.
        /// </summary>
        public Token PayloadKeyword;

        /// <summary>
        /// The as keyword.
        /// </summary>
        public Token AsKeyword;

        /// <summary>
        /// The type node.
        /// </summary>
        public PTypeNode Type;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        public Token RightParenthesisToken;

        /// <summary>
        /// The dot token.
        /// </summary>
        public Token DotToken;

        /// <summary>
        /// The index token.
        /// </summary>
        public Token IndexToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PPayloadReceiveNode()
            : base()
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
            var text = "";

            this.Type.Rewrite(ref position);
            text += "(" + this.Type.GetRewrittenText() + ")";
            text += "this.Payload";
            
            if (this.RightParenthesisToken != null)
            {
                text += this.RightParenthesisToken.TextUnit.Text;

                if (this.DotToken != null)
                {
                    text += this.DotToken.TextUnit.Text;

                    var index = Int32.Parse(this.IndexToken.TextUnit.Text) + 1;
                    text += "Item" + index;
                }
            }

            base.RewrittenTextUnit = new TextUnit(text, this.PayloadKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.PayloadKeyword.TextUnit.Text;
            text += " ";

            text += this.AsKeyword.TextUnit.Text;
            text += " ";

            this.Type.GenerateTextUnit();
            text += this.Type.GetFullText();

            if (this.RightParenthesisToken != null)
            {
                text += this.RightParenthesisToken.TextUnit.Text;

                if (this.DotToken != null)
                {
                    text += this.DotToken.TextUnit.Text;
                    text += this.IndexToken.TextUnit.Text;
                }
            }

            base.TextUnit = new TextUnit(text, this.PayloadKeyword.TextUnit.Line,
                this.PayloadKeyword.TextUnit.Start);
        }

        #endregion
    }
}
