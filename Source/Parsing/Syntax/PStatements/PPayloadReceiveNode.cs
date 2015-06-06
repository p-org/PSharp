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
    internal sealed class PPayloadReceiveNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The payload keyword.
        /// </summary>
        internal Token PayloadKeyword;

        /// <summary>
        /// The as keyword.
        /// </summary>
        internal Token AsKeyword;

        /// <summary>
        /// The actual type.
        /// </summary>
        internal PBaseType Type;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The dot token.
        /// </summary>
        internal Token DotToken;

        /// <summary>
        /// The index token.
        /// </summary>
        internal Token IndexToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isModel">Is a model</param>
        internal PPayloadReceiveNode(bool isModel)
            : base(isModel)
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
            var text = "";

            this.Type.Rewrite();
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

            base.TextUnit = new TextUnit(text, this.PayloadKeyword.TextUnit.Line);
        }

        #endregion
    }
}
