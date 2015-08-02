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

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Payload send expression node.
    /// </summary>
    internal sealed class PPayloadSendExpressionNode : PExpressionNode
    {
        #region fields

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal PPayloadSendExpressionNode(IPSharpProgram program, BlockSyntax node)
            : base(program, node)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Implements specialised rewritting functionality.
        /// </summary>
        protected override void RunSpecialisedRewrittingPass()
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
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text)) as PFieldDeclaration;
            if (field.Type.Type != PType.Tuple &&
                field.Type.Type != PType.Seq &&
                field.Type.Type != PType.Map)
            {
                return;
            }

            var textUnit = new TextUnit("(", this.RewrittenStmtTokens[this.Index].TextUnit.Line);
            this.RewrittenStmtTokens.Insert(this.Index, new Token(textUnit));
            this.Index++;

            var cloneStr = ".Clone() as " + field.Type.GetRewrittenText() + ")";
            textUnit = new TextUnit(cloneStr, this.RewrittenStmtTokens[this.Index].TextUnit.Line);

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
