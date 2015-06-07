//-----------------------------------------------------------------------
// <copyright file="EntryDeclarationNode.cs">
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
    /// Entry declaration node.
    /// </summary>
    internal sealed class EntryDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The entry keyword.
        /// </summary>
        internal Token EntryKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal StatementBlockNode StatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="isModel">Is a model</param>
        internal EntryDeclarationNode(IPSharpProgram program, bool isModel)
            : base(program, isModel)
        {

        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            this.StatementBlock.Rewrite();

            var text = "protected override void OnEntry()";
            text += StatementBlock.TextUnit.Text;

            base.TextUnit = new TextUnit(text, this.EntryKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.StatementBlock.Model();

            var text = "protected override void OnEntry()";
            text += StatementBlock.TextUnit.Text;

            base.TextUnit = new TextUnit(text, this.EntryKeyword.TextUnit.Line);
        }

        #endregion
    }
}
