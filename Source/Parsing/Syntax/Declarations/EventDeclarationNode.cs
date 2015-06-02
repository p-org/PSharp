//-----------------------------------------------------------------------
// <copyright file="EventDeclarationNode.cs">
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
    /// Event declaration node.
    /// </summary>
    public sealed class EventDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The event keyword.
        /// </summary>
        public Token EventKeyword;

        /// <summary>
        /// The modifier token.
        /// </summary>
        public Token Modifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The colon token.
        /// </summary>
        public Token ColonToken;

        /// <summary>
        /// The payload type node.
        /// </summary>
        public PTypeNode PayloadType;

        /// <summary>
        /// The assert keyword.
        /// </summary>
        public Token AssertKeyword;

        /// <summary>
        /// The assume keyword.
        /// </summary>
        public Token AssumeKeyword;

        /// <summary>
        /// The assert value.
        /// </summary>
        public int AssertValue;

        /// <summary>
        /// The assume value.
        /// </summary>
        public int AssumeValue;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        public Token SemicolonToken;

        #endregion

        #region public API

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
            var text = "";
            var initToken = this.EventKeyword;

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            text += "class " + this.Identifier.TextUnit.Text + " : Event";

            text += "\n";
            text += "{\n";
            text += " public " + this.Identifier.TextUnit.Text + "(params Object[] payload)\n";

            text += "  : base(";

            if (this.AssertKeyword != null)
            {
                text += this.AssertValue + ", -1, ";
            }
            else if (this.AssumeKeyword != null)
            {
                text += "-1, " + this.AssumeValue + ", ";
            }
            else
            {
                text += "-1, -1, ";
            }
            
            text += "payload)\n";
            text += " { }\n";
            text += "}\n";

            base.RewrittenTextUnit = new TextUnit(text, initToken.TextUnit.Line, position);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = "";
            var initToken = this.EventKeyword;

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

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

            base.TextUnit = new TextUnit(text, initToken.TextUnit.Line, initToken.TextUnit.Start);
        }

        #endregion
    }
}
