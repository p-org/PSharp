//-----------------------------------------------------------------------
// <copyright file="PEventDeclarationNode.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax.P
{
    /// <summary>
    /// P event declaration node.
    /// </summary>
    public sealed class PEventDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The event keyword.
        /// </summary>
        public Token EventKeyword;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The colon token.
        /// </summary>
        public Token ColonToken;

        /// <summary>
        /// The payload type identifier node.
        /// </summary>
        public PTypeIdentifierNode PayloadType;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        public Token SemicolonToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PEventDeclarationNode()
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
            var text = "class " + this.Identifier.TextUnit.Text + " : Event";

            text += "\n";
            text += "{\n";
            text += " public " + this.Identifier.TextUnit.Text + "(params Object[] payload)\n";
            text += "  : base(payload)\n";
            text += " { }\n";
            text += "}\n";

            base.RewrittenTextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line, position);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = "";

            text += this.EventKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            if (this.ColonToken != null)
            {
                text += " ";
                text += this.ColonToken.TextUnit.Text;
                text += " ";

                this.PayloadType.GenerateTextUnit();
                text += this.PayloadType.GetFullText();
            }

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line,
                this.EventKeyword.TextUnit.Start);
        }

        #endregion
    }
}
