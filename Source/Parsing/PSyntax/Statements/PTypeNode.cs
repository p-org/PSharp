//-----------------------------------------------------------------------
// <copyright file="PTypeNode.cs">
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
    /// Type node.
    /// </summary>
    public sealed class PTypeNode : PSyntaxNode
    {
        #region fields

        /// <summary>
        /// The type tokens.
        /// </summary>
        public List<Token> TypeTokens;

        /// <summary>
        /// The rewritten type tokens.
        /// </summary>
        public List<Token> RewrittenTypeTokens;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PTypeNode()
            : base()
        {
            this.TypeTokens = new List<Token>();
            this.RewrittenTypeTokens = new List<Token>();
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
            if (this.TypeTokens.Count == 0)
            {
                return;
            }

            var start = position;
            var text = "";

            this.Index = 0;
            this.RewrittenTypeTokens = this.TypeTokens.ToList();
            this.RewriteTypeTokens();
            foreach (var tok in this.RewrittenTypeTokens)
            {
                text += tok.TextUnit.Text;
            }

            base.RewrittenTextUnit = new TextUnit(text, this.RewrittenTypeTokens.First().TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            if (this.TypeTokens.Count == 0)
            {
                return;
            }

            var text = "";

            foreach (var tok in this.TypeTokens)
            {
                text += tok.TextUnit.Text;
            }

            base.TextUnit = new TextUnit(text, this.TypeTokens.First().TextUnit.Line,
                this.TypeTokens.First().TextUnit.Start);
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites a type.
        /// </summary>
        private void RewriteTypeTokens()
        {
            if (this.RewrittenTypeTokens[this.Index].Type == TokenType.MachineDecl)
            {
                var textUnit = new TextUnit("Machine", this.RewrittenTypeTokens[this.Index].TextUnit.Line,
                    this.RewrittenTypeTokens[this.Index].TextUnit.Start);
                this.RewrittenTypeTokens[this.Index] = new Token(textUnit, TokenType.MachineDecl);
            }
            else if (this.RewrittenTypeTokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                this.RewriteTupleTypeTokens();
            }

            this.Index++;
        }

        /// <summary>
        /// Rewrites a type tuple.
        /// </summary>
        private void RewriteTupleTypeTokens()
        {
            var tupleIdx = this.Index;
            this.Index++;

            bool expectsComma = false;
            while (this.Index < this.RewrittenTypeTokens.Count &&
                this.RewrittenTypeTokens[this.Index].Type != TokenType.RightParenthesis)
            {
                if (!expectsComma &&
                    (this.RewrittenTypeTokens[this.Index].Type != TokenType.MachineDecl &&
                    this.RewrittenTypeTokens[this.Index].Type != TokenType.Int &&
                    this.RewrittenTypeTokens[this.Index].Type != TokenType.Bool &&
                    this.RewrittenTypeTokens[this.Index].Type != TokenType.LeftParenthesis) ||
                    (expectsComma && this.RewrittenTypeTokens[this.Index].Type != TokenType.Comma))
                {
                    break;
                }

                if (this.RewrittenTypeTokens[this.Index].Type == TokenType.MachineDecl ||
                    this.RewrittenTypeTokens[this.Index].Type == TokenType.Int ||
                    this.RewrittenTypeTokens[this.Index].Type == TokenType.Bool ||
                    this.RewrittenTypeTokens[this.Index].Type == TokenType.LeftParenthesis)
                {
                    this.RewriteTypeTokens();
                    expectsComma = true;
                }
                else if (this.RewrittenTypeTokens[this.Index].Type == TokenType.Comma)
                {
                    this.Index++;
                    expectsComma = false;
                }
            }

            var leftTextUnit = new TextUnit("Tuple<", this.RewrittenTypeTokens[tupleIdx].TextUnit.Line,
                this.RewrittenTypeTokens[tupleIdx].TextUnit.Start);
            this.RewrittenTypeTokens[tupleIdx] = new Token(leftTextUnit);

            var rightTextUnit = new TextUnit(">", this.RewrittenTypeTokens[this.Index].TextUnit.Line,
                this.RewrittenTypeTokens[this.Index].TextUnit.Start);
            this.RewrittenTypeTokens[this.Index] = new Token(rightTextUnit);
        }

        #endregion
    }
}
