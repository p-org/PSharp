//-----------------------------------------------------------------------
// <copyright file="PFieldDeclaration.cs">
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
    /// Field declaration syntax node.
    /// </summary>
    internal sealed class PFieldDeclaration : FieldDeclaration
    {
        #region fields

        /// <summary>
        /// The field keyword.
        /// </summary>
        internal Token FieldKeyword;

        /// <summary>
        /// The colon token.
        /// </summary>
        internal Token ColonToken;

        /// <summary>
        /// The actual type.
        /// </summary>
        internal PBaseType Type;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="machineNode">PMachineDeclarationNode</param>
        /// <param name="isModel">Is a model</param>
        internal PFieldDeclaration(IPSharpProgram program, MachineDeclaration machineNode,
            bool isModel)
            : base(program, machineNode, isModel)
        {

        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            var text = this.GetRewrittenFieldDeclaration();
            base.TextUnit = new TextUnit(text, this.FieldKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenFieldDeclaration();
            base.TextUnit = new TextUnit(text, this.FieldKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten method declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenFieldDeclaration()
        {
            var text = "";

            this.Type.Rewrite();
            text += this.Type.GetRewrittenText();

            text += " ";
            text += this.Identifier.TextUnit.Text;

            if (this.Type.Type == PType.Tuple ||
                this.Type.Type == PType.Seq ||
                this.Type.Type == PType.Map)
            {
                text += " = new " + this.Type.GetRewrittenText() + "()";
            }

            text += this.SemicolonToken.TextUnit.Text + "\n";
            return text;
        }

        #endregion
    }
}
