//-----------------------------------------------------------------------
// <copyright file="PPayloadSendExpressionNode.cs">
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

using System.Linq;

namespace Microsoft.PSharp.Parsing.PSyntax
{
    /// <summary>
    /// Payload send expression node.
    /// </summary>
    public sealed class PPayloadSendExpressionNode : PExpressionNode
    {
        #region fields

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        public PPayloadSendExpressionNode(PStatementBlockNode node)
            : base(node)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Implements specialised rewritting functionality.
        /// </summary>
        /// <param name="position">Position</param>
        protected override void RunSpecialisedRewrittingPass(ref int position)
        {
            this.Index = 0;
            if (base.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                base.RewriteTuple(ref this.Index);
            }

            this.Index = 0;
            while (this.Index < this.RewrittenStmtTokens.Count)
            {
                this.RewriteCloneablePayload();
                this.Index++;
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the cloneable payload.
        /// </summary>
        private void RewriteCloneablePayload()
        {
            if (this.Parent.Machine == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            var field = this.Parent.Machine.FieldDeclarations.Find(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text));
            if (field.TypeNode.Type.Type != PType.Tuple &&
                field.TypeNode.Type.Type != PType.Seq)
            {
                return;
            }

            var cloneStr = ".Clone() as " + field.TypeNode.GetRewrittenText();
            var textUnit = new TextUnit(cloneStr, this.RewrittenStmtTokens[this.Index].TextUnit.Line,
                this.RewrittenStmtTokens[this.Index].TextUnit.Start + cloneStr.Length);

            if (this.Index + 1 == this.RewrittenStmtTokens.Count)
            {
                this.RewrittenStmtTokens.Add(new Token(textUnit));
            }
            else
            {
                this.RewrittenStmtTokens.Insert(this.Index + 1, new Token(textUnit));
            }
        }

        #endregion
    }
}
