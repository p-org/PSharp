//-----------------------------------------------------------------------
// <copyright file="BreakStatement.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Break statement syntax node.
    /// </summary>
    internal sealed class BreakStatement : Statement
    {
        #region fields

        /// <summary>
        /// The break keyword.
        /// </summary>
        internal Token BreakKeyword;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal BreakStatement(IPSharpProgram program, BlockSyntax node)
            : base(program, node)
        {

        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            var text = this.GetRewrittenAssertStatement();

            base.TextUnit = new TextUnit(text, this.BreakKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenAssertStatement();

            base.TextUnit = new TextUnit(text, this.BreakKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten assert statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenAssertStatement()
        {
            var text = "break";
            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
