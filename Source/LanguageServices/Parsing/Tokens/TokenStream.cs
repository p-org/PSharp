//-----------------------------------------------------------------------
// <copyright file="TokenStream.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// P# syntax token stream.
    /// </summary>
    public sealed class TokenStream
    {
        #region fields

        /// <summary>
        /// List of tokens in the stream.
        /// </summary>
        private List<Token> Tokens;

        /// <summary>
        /// The current index of the stream.
        /// </summary>
        public int Index;

        /// <summary>
        /// The length of the stream.
        /// </summary>
        public int Length { get { return this.Tokens.Count; } }

        /// <summary>
        /// True if no tokens remaining in the stream.
        /// </summary>
        public bool Done
        {
            // In some cases of early end of string (e.g. VS Lang Service parsing) we may increment this twice.
            get { return this.Index >= this.Length; }
        }

        /// <summary>
        /// The program this token stream belongs to.
        /// </summary>
        internal IPSharpProgram Program;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public TokenStream(List<Token> tokens)
        {
            this.Tokens = tokens.ToList();
            this.Index = 0;
        }

        /// <summary>
        /// Returns the next token in the stream and progresses by one token.
        /// Returns null if the stream is empty.
        /// </summary>
        /// <returns>Token</returns>
        public Token Next()
        {
            return this.Done ? null : this.Tokens[this.Index++];
        }

        /// <summary>
        /// Returns the next token in the stream without progressing to the next token.
        /// Returns null if the stream is empty.
        /// </summary>
        /// <returns>Token</returns>
        public Token Peek()
        {
            return this.Done ? null : this.Tokens[this.Index];
        }

        /// <summary>
        /// Returns the type of the most recent non-whitespace token, to help refine the
        /// list of expected tokens in the event of error.
        /// </summary>
        /// <returns>Token</returns>
        public TokenType PrevNonWhitespaceType()
        {
            for (var ii = this.Index - 1; ii >= 0; --ii)
            {
                var tokType = this.Tokens[ii].Type;
                switch (tokType)
                {
                    case TokenType.WhiteSpace:
                    case TokenType.NewLine:
                        continue;
                    default:
                        return tokType;
                }
            }
            return TokenType.None;
        }

        /// <summary>
        /// Swaps the current token with a new token containing updated text and type.
        /// Does nothing if the stream is empty or the current index is past the end of the stream.
        /// </summary>
        public void Swap(TextUnit updatedText, TokenType updatedType = Token.DefaultTokenType)
        {
            if (!this.Done)
            {
                this.Tokens[this.Index] = new Token(this.Peek(), updatedText, updatedType);
            }
        }

        /// <summary>
        /// Swaps the current token with a new token containing updated type.
        /// Does nothing if the stream is empty or the current index is past the end of the stream.
        /// </summary>
        public void Swap(TokenType updatedType)
        {
            if (!this.Done)
            {
                this.Tokens[this.Index] = new Token(this.Peek(), updatedType);
            }
        }

        /// <summary>
        /// Returns the token in the given index of the stream. Returns
        /// null if the index is out of bounds.
        /// </summary>
        /// <returns>Token</returns>
        public Token GetAt(int index)
        {
            return (index >= this.Tokens.Count || index < 0) ? null : this.Tokens[index];
        }

        /// <summary>
        /// Skips whitespace and comment tokens.
        /// </summary>
        /// <returns>Skipped tokens</returns>
        public List<Token> SkipWhiteSpaceAndCommentTokens()
        {
            var skipped = new List<Token>();
            while (!this.Done)
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
        /// Skips comment tokens.
        /// </summary>
        public void SkipCommentTokens()
        {
            while (!this.Done)
            {
                var repeat = this.CommentOutLineComment();
                repeat = repeat || this.CommentOutMultiLineComment();

                if (!repeat)
                {
                    break;
                }
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Consumes the next token in the stream. Does nothing 
        /// if the stream is empty.
        /// </summary>
        private void Consume()
        {
            if (this.Index == this.Tokens.Count)
            {
                return;
            }

            this.Tokens.RemoveAt(this.Index);
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        /// <param name="skipped">Skipped tokens</param>
        /// <returns>Boolean</returns>
        private bool SkipWhiteSpaceTokens(List<Token> skipped)
        {
            if ((this.Peek().Type != TokenType.WhiteSpace) &&
                (this.Peek().Type != TokenType.NewLine))
            {
                return false;
            }

            while (!this.Done &&
                (this.Peek().Type == TokenType.WhiteSpace ||
                this.Peek().Type == TokenType.NewLine))
            {
                skipped.Add(this.Next());
            }

            return true;
        }

        /// <summary>
        /// Comments out a line-wide comment, if any.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool CommentOutLineComment()
        {
            if ((this.Peek().Type != TokenType.CommentLine) &&
                (this.Peek().Type != TokenType.Region))
            {
                return false;
            }

            while (!this.Done &&
                this.Peek().Type != TokenType.NewLine)
            {
                this.Consume();
            }

            return true;
        }

        /// <summary>
        /// Comments out a multi-line comment, if any.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool CommentOutMultiLineComment()
        {
            if (this.Peek().Type != TokenType.CommentStart)
            {
                return false;
            }

            while (!this.Done &&
                this.Peek().Type != TokenType.CommentEnd)
            {
                this.Consume();
            }

            this.Consume();

            return true;
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the TokenStream's token count, current index, and current token.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var token = this.Done ? "<done>" : this.Peek().ToString();
            return $"{this.Length}[{this.Index}] {token}";
        }
    }
}
