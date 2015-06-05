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
    internal sealed class EventDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The event keyword.
        /// </summary>
        internal Token EventKeyword;

        /// <summary>
        /// The modifier token.
        /// </summary>
        internal Token Modifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The colon token.
        /// </summary>
        internal Token ColonToken;

        /// <summary>
        /// The payload type.
        /// </summary>
        internal PBaseType PayloadType;

        /// <summary>
        /// The assert keyword.
        /// </summary>
        internal Token AssertKeyword;

        /// <summary>
        /// The assume keyword.
        /// </summary>
        internal Token AssumeKeyword;

        /// <summary>
        /// The assert value.
        /// </summary>
        internal int AssertValue;

        /// <summary>
        /// The assume value.
        /// </summary>
        internal int AssumeValue;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        #endregion

        #region internal API

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
            text += " internal " + this.Identifier.TextUnit.Text + "(params Object[] payload)\n";

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

            base.TextUnit = new TextUnit(text, initToken.TextUnit.Line);
        }

        #endregion
    }
}
