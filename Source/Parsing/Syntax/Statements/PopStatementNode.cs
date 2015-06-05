//-----------------------------------------------------------------------
// <copyright file="PopStatementNode.cs">
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
    /// Pop statement node.
    /// </summary>
    internal sealed class PopStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The pop keyword.
        /// </summary>
        internal Token PopKeyword;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        internal PopStatementNode(StatementBlockNode node)
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
            var text = "{\n";
            text += "this.Pop()";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            text += "return;\n";
            text += "}\n";

            base.TextUnit = new TextUnit(text, this.PopKeyword.TextUnit.Line);
        }

        #endregion
    }
}
