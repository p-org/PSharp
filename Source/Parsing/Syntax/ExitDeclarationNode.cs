//-----------------------------------------------------------------------
// <copyright file="ExitDeclarationNode.cs">
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
    /// Exit declaration node.
    /// </summary>
    public sealed class ExitDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The exit keyword.
        /// </summary>
        public Token ExitKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        public StatementBlockNode StatementBlock;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExitDeclarationNode()
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
            this.StatementBlock.Rewrite(ref position);

            var text = "protected override void OnExit()";

            text += StatementBlock.GetRewrittenText();

            base.RewrittenTextUnit = new TextUnit(text, this.ExitKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            this.StatementBlock.GenerateTextUnit();

            var text = this.ExitKeyword.TextUnit.Text;

            text += this.StatementBlock.GetFullText();

            base.TextUnit = new TextUnit(text, this.ExitKeyword.TextUnit.Line,
                this.ExitKeyword.TextUnit.Start);
        }

        #endregion
    }
}
