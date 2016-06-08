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
        public int Length
        {
            get
            {
                return this.Tokens.Count;
            }
        }

        /// <summary>
        /// True if no tokens remaining in the stream.
        /// </summary>
        public bool Done
        {
            get
            {
                return this.Index == this.Length;
            }
        }

        /// <summary>
        /// The name of the currently parsed machine.
        /// </summary>
        internal string CurrentMachine;

        /// <summary>
        /// The name of the currently parsed state.
        /// </summary>
        internal string CurrentState;

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
            this.CurrentMachine = "";
            this.CurrentState = "";
        }

        /// <summary>
        /// Returns the next token in the stream and progresses by one token.
        /// Returns null if the stream is empty.
        /// </summary>
        /// <returns>Token</returns>
        public Token Next()
        {
            if (this.Index == this.Tokens.Count)
            {
                return null;
            }

            var token = this.Tokens[this.Index];
            this.Index++;

            return token;
        }

        /// <summary>
        /// Returns the next token in the stream without progressing to the next token.
        /// Returns null if the stream is empty.
        /// </summary>
        /// <returns>Token</returns>
        public Token Peek()
        {
            if (this.Index == this.Tokens.Count)
            {
                return null;
            }
            
            return this.Tokens[this.Index];
        }

        /// <summary>
        /// Swaps the current token with the new token. Does nothing if the stream is
        /// empty.
        /// </summary>
        public void Swap(Token token)
        {
            if (this.Index == this.Tokens.Count)
            {
                return;
            }

            this.Tokens[this.Index] = token;
        }

        /// <summary>
        /// Returns the token in the given index of the stream. Returns
        /// null if the index is out of bounds.
        /// </summary>
        /// <returns>Token</returns>
        public Token GetAt(int index)
        {
            if (index >= this.Tokens.Count || index < 0)
            {
                return null;
            }
            
            return this.Tokens[index];
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
    }
}
