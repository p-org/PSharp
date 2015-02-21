//-----------------------------------------------------------------------
// <copyright file="BaseParser.cs">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Abstract parser.
    /// </summary>
    public abstract class BaseParser : IParser
    {
        #region fields

        /// <summary>
        /// List of original tokens.
        /// </summary>
        protected List<Token> OriginalTokens;

        /// <summary>
        /// List of tokens.
        /// </summary>
        protected List<Token> Tokens;

        /// <summary>
        /// The current index.
        /// </summary>
        protected int Index;

        /// <summary>
        /// The name of the currently parsed machine.
        /// </summary>
        protected string CurrentMachine;

        /// <summary>
        /// The name of the currently parsed state.
        /// </summary>
        protected string CurrentState;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        public BaseParser()
        {
            
        }

        /// <summary>
        /// Returns the parsed tokens.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <returns>Parsed tokens</returns>
        public List<Token> ParseTokens(List<Token> tokens)
        {
            this.OriginalTokens = tokens.ToList();
            this.Tokens = tokens;
            this.Index = 0;
            this.CurrentMachine = "";
            this.CurrentState = "";

            try
            {
                this.ParseNextToken();
            }
            catch (ParsingException ex)
            {
                ErrorReporter.ReportErrorAndExit(ex.Message);
            }
            
            return this.Tokens;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected abstract void ParseNextToken();

        /// <summary>
        /// Returns the first previous non-whitespace token.
        /// </summary>
        /// <returns>Token and its index</returns>
        protected Tuple<Token, int> GetPreviousToken()
        {
            int currentIdx = this.Index - 1;
            while (currentIdx >= 0 &&
                (this.Tokens[currentIdx].Type == TokenType.WhiteSpace ||
                this.Tokens[currentIdx].Type == TokenType.NewLine))
            {
                currentIdx--;
            }

            if (currentIdx < 0)
            {
                return null;
            }

            return new Tuple<Token, int>(this.Tokens[currentIdx], currentIdx);
        }

        /// <summary>
        /// Skips whitespace and comment tokens.
        /// </summary>
        protected void SkipWhiteSpaceAndCommentTokens()
        {
            while (this.Index < this.Tokens.Count)
            {
                var repeat = this.EraseLineComment();
                repeat = repeat || this.EraseMultiLineComment();
                repeat = repeat || this.SkipWhiteSpaceTokens();

                if (!repeat)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Erases whitespace and comment tokens.
        /// </summary>
        protected void EraseWhiteSpaceAndCommentTokens()
        {
            while (this.Index < this.Tokens.Count)
            {
                var repeat = this.EraseLineComment();
                repeat = repeat || this.EraseMultiLineComment();
                repeat = repeat || this.EraseWhiteSpaceTokens();

                if (!repeat)
                {
                    break;
                }
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        private bool SkipWhiteSpaceTokens()
        {
            if ((this.Tokens[this.Index].Type != TokenType.WhiteSpace) &&
                (this.Tokens[this.Index].Type != TokenType.NewLine))
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                (this.Tokens[this.Index].Type == TokenType.WhiteSpace ||
                this.Tokens[this.Index].Type == TokenType.NewLine))
            {
                this.Index++;
            }

            if (this.Index == this.Tokens.Count)
            {
                throw new ParsingException("unexpected end of token list.");
            }

            return true;
        }

        /// <summary>
        /// Erases a line-wide comment, if any.
        /// </summary>
        private bool EraseLineComment()
        {
            if ((this.Tokens[this.Index].Type != TokenType.Comment) &&
                (this.Tokens[this.Index].Type != TokenType.Region))
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.NewLine)
            {
                this.Tokens.RemoveAt(this.Index);
            }

            if (this.Index == this.Tokens.Count)
            {
                throw new ParsingException("unexpected end of token list.");
            }

            return true;
        }

        /// <summary>
        /// Erases a multi-line comment, if any.
        /// </summary>
        private bool EraseMultiLineComment()
        {
            if (this.Tokens[this.Index].Type != TokenType.CommentStart)
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.CommentEnd)
            {
                this.Tokens.RemoveAt(this.Index);
            }

            if (this.Index == this.Tokens.Count)
            {
                throw new ParsingException("unexpected end of token list.");
            }

            this.Tokens.RemoveAt(this.Index);

            return true;
        }

        /// <summary>
        /// Erases whitespace tokens.
        /// </summary>
        private bool EraseWhiteSpaceTokens()
        {
            if (this.Tokens[this.Index].Type != TokenType.WhiteSpace)
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type == TokenType.WhiteSpace)
            {
                this.Tokens.RemoveAt(this.Index);
            }

            if (this.Index == this.Tokens.Count)
            {
                throw new ParsingException("unexpected end of token list.");
            }

            return true;
        }

        #endregion
    }
}
