//-----------------------------------------------------------------------
// <copyright file="ExpressionNode.cs">
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
    /// Expression node.
    /// </summary>
    public class ExpressionNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The block node.
        /// </summary>
        public readonly StatementBlockNode Parent;

        /// <summary>
        /// The statement tokens.
        /// </summary>
        public List<Token> StmtTokens;

        /// <summary>
        /// The rewritten statement tokens.
        /// </summary>
        public List<Token> RewrittenStmtTokens;

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
        public ExpressionNode(StatementBlockNode node)
            : base()
        {
            this.Parent = node;
            this.StmtTokens = new List<Token>();
            this.RewrittenStmtTokens = new List<Token>();
        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            if (this.StmtTokens.Count == 0)
            {
                return "";
            }

            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            if (this.StmtTokens.Count == 0)
            {
                return "";
            }

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
            if (this.StmtTokens.Count == 0)
            {
                return;
            }

            this.Index = 0;
            this.RewrittenStmtTokens = this.StmtTokens.ToList();
            this.RewriteNextToken(ref position);

            var start = position;
            var text = "";

            foreach (var token in this.RewrittenStmtTokens)
            {
                text += token.TextUnit.Text;
            }

            base.RewrittenTextUnit = new TextUnit(text,
                this.StmtTokens.First().TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            if (this.StmtTokens.Count == 0)
            {
                return;
            }

            var text = "";

            foreach (var tok in this.StmtTokens)
            {
                text += tok.TextUnit.Text;
            }

            base.TextUnit = new TextUnit(text, this.StmtTokens.First().TextUnit.Line,
                this.StmtTokens.First().TextUnit.Start);
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the next token.
        /// </summary>
        /// <param name="position">Position</param>
        private void RewriteNextToken(ref int position)
        {
            if (this.Index == this.RewrittenStmtTokens.Count)
            {
                return;
            }

            var token = this.RewrittenStmtTokens[this.Index];
            if (token.Type == TokenType.Payload)
            {
                this.RewritePayload(ref position);
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis(ref position);
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteIdentifier(ref position);
            }

            this.Index++;
            this.RewriteNextToken(ref position);
        }

        /// <summary>
        /// Rewrites the payload.
        /// </summary>
        /// param name="position">Position</param>
        private void RewritePayload(ref int position)
        {
            var startIdx = this.Index;
            this.Index++;

            var isArray = false;
            this.SkipWhiteSpaceTokens();
            if (this.Index < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[this.Index].Type == TokenType.LeftSquareBracket)
            {
                isArray = true;
            }

            this.Index = startIdx;

            if (isArray)
            {
                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "((Object[])this.Payload)";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
                position += text.Length;
            }
            else
            {
                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "this.Payload";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
                position += text.Length;
            }
        }

        /// <summary>
        /// Rewrites the this keyword.
        /// </summary>
        /// param name="position">Position</param>
        private void RewriteThis(ref int position)
        {
            if (this.Parent.State == null)
            {
                return;
            }

            var removeIdx = this.Index + 1;
            this.SkipWhiteSpaceTokens();

            if (removeIdx < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[removeIdx].Type == TokenType.Dot)
            {
                this.RewrittenStmtTokens.RemoveAt(this.Index);
                this.RewrittenStmtTokens.RemoveAt(this.Index);
                this.Index--;
            }
            else
            {
                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "this.Machine";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
                position += text.Length;
            }
        }

        /// <summary>
        /// Rewrites the identifier.
        /// </summary>
        /// <param name="position">Position</param>
        private void RewriteIdentifier(ref int position)
        {
            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text)) ||
                this.Parent.Machine.ActionDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text)) ||
                this.Parent.Machine.MethodDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "(this.Machine as " + this.Parent.Machine.Identifier.TextUnit.Text + ").";
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line, position)));
            position += text.Length;
            this.Index++;
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        private void SkipWhiteSpaceTokens()
        {
            while (this.Index < this.RewrittenStmtTokens.Count &&
                (this.RewrittenStmtTokens[this.Index].Type == TokenType.WhiteSpace ||
                this.RewrittenStmtTokens[this.Index].Type == TokenType.NewLine))
            {
                this.Index++;
            }

            return;
        }

        #endregion
    }
}
