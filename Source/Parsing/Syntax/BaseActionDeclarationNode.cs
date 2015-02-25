//-----------------------------------------------------------------------
// <copyright file="StateActionDeclarationNode.cs">
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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Base action declaration node.
    /// </summary>
    public abstract class BaseActionDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The machine parent node.
        /// </summary>
        public readonly MachineDeclarationNode Machine;

        /// <summary>
        /// The state parent node.
        /// </summary>
        public readonly StateDeclarationNode State;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        public Token LeftCurlyBracketToken;

        /// <summary>
        /// List of statement tokens.
        /// </summary>
        public List<Token> Statements;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        public Token RightCurlyBracketToken;

        /// <summary>
        /// Rewritten statements.
        /// </summary>
        private List<Token> RewrittenStatements;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="stateNode">StateDeclarationNode</param>
        public BaseActionDeclarationNode(MachineDeclarationNode machineNode, StateDeclarationNode stateNode)
        {
            this.Machine = machineNode;
            this.State = stateNode;
            this.Statements = new List<Token>();
            this.RewrittenStatements = new List<Token>();
        }

        #endregion

        #region protected API

        /// <summary>
        /// Rewrites the statements.
        /// </summary>
        /// <returns>Rewritten statements</returns>
        protected List<Token> RewriteStatements()
        {
            this.Index = 0;
            this.RewrittenStatements = this.Statements.ToList();
            this.RewriteNextToken();
            return this.RewrittenStatements;
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the next token.
        /// </summary>
        private void RewriteNextToken()
        {
            if (this.Index == this.RewrittenStatements.Count)
            {
                return;
            }

            var token = this.RewrittenStatements[this.Index];
            if (token.Type == TokenType.CreateMachine)
            {
                this.RewriteCreateStatement();
            }
            else if (token.Type == TokenType.RaiseEvent)
            {
                this.RewriteRaiseStatement();
            }
            else if (token.Type == TokenType.SendEvent)
            {
                this.RewriteSendStatement();
            }
            else if (token.Type == TokenType.DeleteMachine)
            {
                this.RewriteDeleteStatement();
            }
            else if (token.Type == TokenType.Assert)
            {
                this.RewriteAssertStatement();
            }
            else if (token.Type == TokenType.Payload)
            {
                this.RewritePayload();
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis();
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteIdentifier();
            }

            this.Index++;
            this.RewriteNextToken();
        }

        /// <summary>
        /// Rewrites the create statement.
        /// </summary>
        private void RewriteCreateStatement()
        {
            this.RewrittenStatements[this.Index] = new Token("Machine");
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("Factory"));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("CreateMachine"));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("<", TokenType.LeftAngleBracket));
            this.Index++;

            var startIdx = this.Index;
            var machineIds = new List<Token>();
            var payload = new List<Token>();

            this.SkipWhiteSpaceAndCommentTokens();

            while (this.RewrittenStatements[this.Index].Type != TokenType.Semicolon &&
                this.RewrittenStatements[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                machineIds.Add(this.RewrittenStatements[this.Index]);
                this.Index++;
            }

            while (this.RewrittenStatements[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.Index++;
            }

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();
            while (this.RewrittenStatements[this.Index].Type != TokenType.RightCurlyBracket)
            {
                payload.Add(this.RewrittenStatements[this.Index]);
                this.Index++;
            }

            while (this.RewrittenStatements[this.Index].Type != TokenType.Semicolon)
            {
                this.Index++;
            }

            this.Index--;
            while (this.Index != startIdx)
            {
                this.RewrittenStatements.RemoveAt(this.Index);
                this.Index--;
            }

            foreach (var id in machineIds)
            {
                this.RewrittenStatements.Insert(this.Index, id);
                this.Index++;
            }

            this.RewrittenStatements.Insert(this.Index, new Token(">", TokenType.RightAngleBracket));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
            this.Index++;

            foreach (var item in payload)
            {
                this.RewrittenStatements.Insert(this.Index, item);
                this.Index++;
            }

            this.RewrittenStatements[this.Index] = new Token(")", TokenType.RightParenthesis);
            this.Index = startIdx - 1;
        }

        /// <summary>
        /// Rewrites the raise statement.
        /// </summary>
        private void RewriteRaiseStatement()
        {
            this.RewrittenStatements[this.Index] = new Token("this", TokenType.This);
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("Raise"));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
            this.Index++;

            var startIdx = this.Index;
            var eventId = "";
            var payload = new List<Token>();

            this.SkipWhiteSpaceAndCommentTokens();

            eventId = this.RewrittenStatements[this.Index].Text;

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.RewrittenStatements[this.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.Index++;
                while (this.RewrittenStatements[this.Index].Type != TokenType.RightCurlyBracket)
                {
                    payload.Add(this.RewrittenStatements[this.Index]);
                    this.Index++;
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            while (this.RewrittenStatements[this.Index].Type != TokenType.Semicolon)
            {
                this.Index++;
            }

            while (this.Index != startIdx)
            {
                this.RewrittenStatements.RemoveAt(this.Index);
                this.Index--;
            }

            this.RewrittenStatements.Insert(this.Index, new Token("new", TokenType.New));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(" ", TokenType.WhiteSpace));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(eventId, TokenType.EventIdentifier));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
            this.Index++;

            foreach (var item in payload)
            {
                this.RewrittenStatements.Insert(this.Index, item);
                this.Index++;
            }

            this.RewrittenStatements.Insert(this.Index, new Token(")", TokenType.RightParenthesis));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(")", TokenType.RightParenthesis));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(";", TokenType.Semicolon));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("return", TokenType.Return));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(";", TokenType.Semicolon));
            this.Index = startIdx - 1;
        }

        /// <summary>
        /// Rewrites the send statement.
        /// </summary>
        private void RewriteSendStatement()
        {
            this.RewrittenStatements[this.Index] = new Token("this", TokenType.This);
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("Send"));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
            this.Index++;

            var startIdx = this.Index;
            var eventId = "";
            var machineIds = new List<Token>();
            var payload = new List<Token>();

            this.SkipWhiteSpaceAndCommentTokens();

            eventId = this.RewrittenStatements[this.Index].Text;

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.RewrittenStatements[this.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.Index++;
                while (this.RewrittenStatements[this.Index].Type != TokenType.RightCurlyBracket)
                {
                    payload.Add(this.RewrittenStatements[this.Index]);
                    this.Index++;
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            while (this.RewrittenStatements[this.Index].Type != TokenType.Semicolon)
            {
                machineIds.Add(this.RewrittenStatements[this.Index]);
                this.Index++;
            }

            this.Index--;
            while (this.Index != startIdx)
            {
                this.RewrittenStatements.RemoveAt(this.Index);
                this.Index--;
            }

            foreach (var id in machineIds)
            {
                this.RewrittenStatements.Insert(this.Index, id);
                this.Index++;
            }

            this.RewrittenStatements.Insert(this.Index, new Token(",", TokenType.Comma));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(" ", TokenType.WhiteSpace));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("new", TokenType.New));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(" ", TokenType.WhiteSpace));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(eventId, TokenType.EventIdentifier));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
            this.Index++;

            foreach (var item in payload)
            {
                this.RewrittenStatements.Insert(this.Index, item);
                this.Index++;
            }

            this.RewrittenStatements.Insert(this.Index, new Token(")", TokenType.RightParenthesis));
            this.Index++;

            this.RewrittenStatements[this.Index] = new Token(")", TokenType.RightParenthesis);
            this.Index = startIdx - 1;
        }

        /// <summary>
        /// Rewrites the delete statement.
        /// </summary>
        private void RewriteDeleteStatement()
        {
            this.RewrittenStatements[this.Index] = new Token("this", TokenType.This);
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("Delete"));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(")", TokenType.RightParenthesis));
            this.Index++;

            this.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Rewrites the assert statement.
        /// </summary>
        private void RewriteAssertStatement()
        {
            this.RewrittenStatements[this.Index] = new Token("this", TokenType.This);
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
            this.Index++;

            this.RewrittenStatements.Insert(this.Index, new Token("Assert"));
            this.Index++;
        }

        /// <summary>
        /// Rewrites the payload.
        /// </summary>
        private void RewritePayload()
        {
            var startIdx = this.Index;
            this.Index++;

            var isArray = false;
            this.SkipWhiteSpaceAndCommentTokens();
            if (this.RewrittenStatements[this.Index].Type == TokenType.LeftSquareBracket)
            {
                isArray = true;
            }

            this.Index = startIdx;

            if (isArray)
            {
                this.RewrittenStatements[this.Index] = new Token("(", TokenType.LeftParenthesis);
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("(", TokenType.LeftParenthesis));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("Object"));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("[", TokenType.LeftSquareBracket));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("]", TokenType.RightSquareBracket));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token(")", TokenType.RightParenthesis));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("this", TokenType.This));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("Payload"));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token(")", TokenType.RightParenthesis));
            }
            else
            {
                this.RewrittenStatements[this.Index] = new Token("this", TokenType.This);
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("Payload"));
            }
        }

        /// <summary>
        /// Rewrites the this.
        /// </summary>
        private void RewriteThis()
        {
            if (this.State == null)
            {
                return;
            }

            var replaceIdx = this.Index;
            this.Index++;

            this.SkipWhiteSpaceAndCommentTokens();
            if (this.RewrittenStatements[this.Index].Type == TokenType.Dot)
            {
                this.RewrittenStatements.RemoveAt(this.Index);
                this.RewrittenStatements.RemoveAt(replaceIdx);
                this.Index = replaceIdx - 1;
            }
            else
            {
                this.RewrittenStatements.Insert(this.Index, new Token(".", TokenType.Dot));
                this.Index++;

                this.RewrittenStatements.Insert(this.Index, new Token("Machine", TokenType.Identifier));
            }
        }

        /// <summary>
        /// Rewrites the identifier.
        /// </summary>
        private void RewriteIdentifier()
        {
            if (this.Machine == null || this.State == null ||
                !(this.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStatements[this.Index].Text)) ||
                this.Machine.ActionDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStatements[this.Index].Text)) ||
                this.Machine.MethodDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStatements[this.Index].Text))))
            {
                return;
            }

            var replaceIdx = this.Index;
            this.RewrittenStatements.Insert(replaceIdx, new Token("(", TokenType.LeftParenthesis));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token("this", TokenType.This));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token(".", TokenType.Dot));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token("Machine", TokenType.Identifier));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token("as", TokenType.As));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token(this.Machine.Identifier.TextUnit.Text, TokenType.Identifier));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token(")", TokenType.RightParenthesis));
            replaceIdx++;

            this.RewrittenStatements.Insert(replaceIdx, new Token(".", TokenType.Dot));
            replaceIdx++;

            this.Index = replaceIdx;
        }

        /// <summary>
        /// Skips whitespace and comment tokens.
        /// </summary>
        /// <returns>Skipped tokens</returns>
        private List<Token> SkipWhiteSpaceAndCommentTokens()
        {
            var skipped = new List<Token>();
            while (this.Index < this.RewrittenStatements.Count)
            {
                var repeat = this.CommentOutLineComment();
                repeat = repeat || this.CommentOutMultiLineComment();
                repeat = repeat || this.SkipWhiteSpaceTokens(skipped);

                if (!repeat)
                {
                    break;
                }
            }

            return skipped;
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        /// <param name="skipped">Skipped tokens</param>
        /// <returns>Boolean value</returns>
        private bool SkipWhiteSpaceTokens(List<Token> skipped)
        {
            if ((this.RewrittenStatements[this.Index].Type != TokenType.WhiteSpace) &&
                (this.RewrittenStatements[this.Index].Type != TokenType.NewLine))
            {
                return false;
            }

            while (this.Index < this.RewrittenStatements.Count &&
                (this.RewrittenStatements[this.Index].Type == TokenType.WhiteSpace ||
                this.RewrittenStatements[this.Index].Type == TokenType.NewLine))
            {
                skipped.Add(this.RewrittenStatements[this.Index]);
                this.Index++;
            }

            return true;
        }

        /// <summary>
        /// Comments out a line-wide comment, if any.
        /// </summary>
        /// <returns>Boolean value</returns>
        private bool CommentOutLineComment()
        {
            if ((this.RewrittenStatements[this.Index].Type != TokenType.CommentLine) &&
                (this.RewrittenStatements[this.Index].Type != TokenType.Region))
            {
                return false;
            }

            while (this.Index < this.RewrittenStatements.Count &&
                this.RewrittenStatements[this.Index].Type != TokenType.NewLine)
            {
                this.RewrittenStatements.RemoveAt(this.Index);
            }

            return true;
        }

        /// <summary>
        /// Comments out a multi-line comment, if any.
        /// </summary>
        /// <returns>Boolean value</returns>
        private bool CommentOutMultiLineComment()
        {
            if (this.RewrittenStatements[this.Index].Type != TokenType.CommentStart)
            {
                return false;
            }

            while (this.Index < this.RewrittenStatements.Count &&
                this.RewrittenStatements[this.Index].Type != TokenType.CommentEnd)
            {
                this.RewrittenStatements.RemoveAt(this.Index);
            }

            this.RewrittenStatements.RemoveAt(this.Index);

            return true;
        }

        #endregion
    }
}
