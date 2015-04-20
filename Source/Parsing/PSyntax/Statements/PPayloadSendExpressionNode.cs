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
        }

        #endregion
    }
}
