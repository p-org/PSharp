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
    internal class ExpressionNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The block node.
        /// </summary>
        internal readonly StatementBlockNode Parent;

        /// <summary>
        /// The statement tokens.
        /// </summary>
        internal List<Token> StmtTokens;

        /// <summary>
        /// The rewritten statement tokens.
        /// </summary>
        internal List<Token> RewrittenStmtTokens;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        internal ExpressionNode(StatementBlockNode node)
            : base()
        {
            this.Parent = node;
            this.StmtTokens = new List<Token>();
            this.RewrittenStmtTokens = new List<Token>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            if (this.StmtTokens.Count == 0)
            {
                return "";
            }

            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            if (this.StmtTokens.Count == 0)
            {
                return;
            }

            this.Index = 0;
            this.RewrittenStmtTokens = this.StmtTokens.ToList();
            this.RewriteNextToken();
            
            var text = "";

            foreach (var token in this.RewrittenStmtTokens)
            {
                text += token.TextUnit.Text;
            }

            base.TextUnit = new TextUnit(text, this.StmtTokens.First().TextUnit.Line);
        }

        #endregion

        #region protected API

        /// <summary>
        /// Rewrites the machine type.
        /// </summary>
        protected void RewriteMachineType()
        {
            var textUnit = new TextUnit("Machine", this.RewrittenStmtTokens[this.Index].TextUnit.Line);
            this.RewrittenStmtTokens[this.Index] = new Token(textUnit, this.RewrittenStmtTokens[this.Index].Type);
        }

        /// <summary>
        /// Rewrites the non-deterministic choice.
        /// </summary>
        protected void RewriteNonDeterministicChoice()
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "Microsoft.PSharp.Havoc.Boolean";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the next token.
        /// </summary>
        private void RewriteNextToken()
        {
            if (this.Index == this.RewrittenStmtTokens.Count)
            {
                return;
            }

            var token = this.RewrittenStmtTokens[this.Index];
            if (token.Type == TokenType.Payload)
            {
                this.RewritePayload();
            }
            else if (token.Type == TokenType.MachineDecl)
            {
                this.RewriteMachineType();
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis();
            }
            else if (token.Type == TokenType.NonDeterministic)
            {
                this.RewriteNonDeterministicChoice();
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteIdentifier();
            }

            this.Index++;
            this.RewriteNextToken();
        }

        /// <summary>
        /// Rewrites the payload.
        /// </summary>
        private void RewritePayload()
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
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
            }
            else
            {
                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "this.Payload";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
            }
        }

        /// <summary>
        /// Rewrites the this keyword.
        /// </summary>
        private void RewriteThis()
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
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
            }
        }
        
        /// <summary>
        /// Rewrites the identifier.
        /// </summary>
        private void RewriteIdentifier()
        {
            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text)) ||
                this.Parent.Machine.MethodDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "(this.Machine as " + this.Parent.Machine.Identifier.TextUnit.Text + ").";
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line)));
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
