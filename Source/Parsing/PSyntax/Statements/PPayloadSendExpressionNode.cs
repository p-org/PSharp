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

using System;
using System.Collections.Generic;
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

            var payloadType = "";
            this.RewritePayload(ref payloadType);
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites a payload.
        /// </summary>
        /// <param name="payloadType">Payload type</param>
        private void RewritePayload(ref string payloadType)
        {
            if (base.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                this.RewritePayloadTuple(ref payloadType);
            }
            else if (base.RewrittenStmtTokens[this.Index].Type == TokenType.This)
            {
                payloadType += "Machine";
            }
            else
            {
                var field = base.Parent.Machine.FieldDeclarations.Find(val => val.Identifier.TextUnit.Text.Equals(
                    base.RewrittenStmtTokens[this.Index].TextUnit.Text));
                payloadType += field.Type.GetRewrittenText();
            }

            this.Index++;
        }

        /// <summary>
        /// Rewrites a payload tuple.
        /// </summary>
        /// <param name="payloadType">Payload type</param>
        private void RewritePayloadTuple(ref string payloadType)
        {
            var tupleType = "Tuple<";
            var tupleIdx = this.Index;
            this.Index++;

            bool expectsComma = false;
            while (this.Index < base.RewrittenStmtTokens.Count &&
                base.RewrittenStmtTokens[this.Index].Type != TokenType.RightParenthesis)
            {
                if (!expectsComma &&
                    (base.RewrittenStmtTokens[this.Index].Type != TokenType.Identifier &&
                    base.RewrittenStmtTokens[this.Index].Type != TokenType.LeftParenthesis) ||
                    (expectsComma && base.RewrittenStmtTokens[this.Index].Type != TokenType.Comma))
                {
                    break;
                }

                if (base.RewrittenStmtTokens[this.Index].Type == TokenType.Identifier ||
                    base.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
                {
                    var type = "";
                    this.RewritePayload(ref type);
                    tupleType += type;
                    expectsComma = true;
                }
                else if (base.RewrittenStmtTokens[this.Index].Type == TokenType.Comma)
                {
                    tupleType += ",";
                    expectsComma = false;
                    this.Index++;
                }
            }

            tupleType += ">";
            payloadType += tupleType;

            var tupleStr = "new " + tupleType + "(";
            var textUnit = new TextUnit(tupleStr, base.RewrittenStmtTokens[tupleIdx].TextUnit.Line,
                base.RewrittenStmtTokens[tupleIdx].TextUnit.Start);
            base.RewrittenStmtTokens[tupleIdx] = new Token(textUnit);
        }

        #endregion
    }
}
