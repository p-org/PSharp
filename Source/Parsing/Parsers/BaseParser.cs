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

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// An abstract parser.
    /// </summary>
    public abstract class BaseParser : IParser
    {
        #region fields

        /// <summary>
        /// File path of syntax tree currently parsed.
        /// </summary>
        protected string FilePath;

        /// <summary>
        /// List of original tokens.
        /// </summary>
        protected List<Token> OriginalTokens;

        /// <summary>
        /// A P# program.
        /// </summary>
        protected IPSharpProgram Program;

        protected TokenStream TokenStream;

        /// <summary>
        /// The name of the currently parsed machine.
        /// </summary>
        protected string CurrentMachine;

        /// <summary>
        /// The name of the currently parsed state.
        /// </summary>
        protected string CurrentState;

        /// <summary>
        /// List of expected token types at end of parsing.
        /// </summary>
        protected List<TokenType> ExpectedTokenTypes;

        /// <summary>
        /// True if the parser is running internally and not from
        /// visual studio or another external tool.
        /// Else false.
        /// </summary>
        private bool IsRunningInternally;

        #endregion

        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BaseParser()
        {
            this.IsRunningInternally = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        internal BaseParser(string filePath)
        {
            this.FilePath = filePath;
            this.IsRunningInternally = true;
        }

        /// <summary>
        /// Returns a P# program.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <returns>P# program</returns>
        public IPSharpProgram ParseTokens(List<Token> tokens)
        {
            this.OriginalTokens = tokens.ToList();
            this.Program = this.CreateNewProgram();
            this.TokenStream = new TokenStream(tokens);
            this.CurrentMachine = "";
            this.CurrentState = "";
            this.ExpectedTokenTypes = new List<TokenType>();

            try
            {
                this.ParseNextToken();
            }
            catch (EndOfTokensException ex)
            {
                this.ExpectedTokenTypes = ex.ExpectedTokenTypes;
            }
            catch (ParsingException ex)
            {
                ErrorReporter.ReportErrorAndExit(ex.Message);
            }

            this.Program.GenerateTextUnits();
            return this.Program;
        }

        /// <summary>
        /// Returns the expected token types at the end of parsing.
        /// </summary>
        /// <returns>Expected token types</returns>
        public List<TokenType> GetExpectedTokenTypes()
        {
            return this.ExpectedTokenTypes;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns a new P# program.
        /// </summary>
        /// <returns>P# program</returns>
        protected abstract IPSharpProgram CreateNewProgram();

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected abstract void ParseNextToken();

        /// <summary>
        /// Reports a parsing error. Only works if the parser is
        /// running internally.
        /// </summary>
        /// <param name="error">Error</param>
        protected void ReportParsingError(string error)
        {
            if (!this.IsRunningInternally)
            {
                return;
            }

            var errorIndex = this.TokenStream.Index;
            if (this.TokenStream.Index == this.TokenStream.Length &&
                this.TokenStream.Index > 0)
            {
                errorIndex--;
            }

            var errorToken = this.TokenStream.GetAt(errorIndex);
            var errorLine = this.OriginalTokens.Where(
                val => val.TextUnit.Line == errorToken.TextUnit.Line).ToList();

            error += "\nIn " + this.FilePath + " (line " + errorToken.TextUnit.Line + "):\n";

            int nonWhiteIndex = 0;
            for (int idx = 0; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Type != TokenType.WhiteSpace)
                {
                    nonWhiteIndex = idx;
                    break;
                }
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                error += errorLine[idx].TextUnit.Text;
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Equals(errorToken) && errorIndex == this.TokenStream.Index)
                {
                    error += new StringBuilder().Append('~', errorLine[idx].TextUnit.Text.Length);
                    break;
                }
                else
                {
                    error += new StringBuilder().Append(' ', errorLine[idx].TextUnit.Text.Length);
                }
            }

            if (errorIndex != this.TokenStream.Index)
            {
                error += "^";
            }

            ErrorReporter.ReportErrorAndExit(error);
        }

        /// <summary>
        /// Skips whitespace and comment tokens.
        /// </summary>
        /// <returns>Skipped tokens</returns>
        protected List<Token> SkipWhiteSpaceAndCommentTokens()
        {
            var skipped = new List<Token>();
            while (!this.TokenStream.Done)
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
        protected void SkipCommentTokens()
        {
            while (!this.TokenStream.Done)
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

        #region private API

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        /// <param name="skipped">Skipped tokens</param>
        /// <returns>Boolean value</returns>
        private bool SkipWhiteSpaceTokens(List<Token> skipped)
        {
            if ((this.TokenStream.Peek().Type != TokenType.WhiteSpace) &&
                (this.TokenStream.Peek().Type != TokenType.NewLine))
            {
                return false;
            }

            while (!this.TokenStream.Done &&
                (this.TokenStream.Peek().Type == TokenType.WhiteSpace ||
                this.TokenStream.Peek().Type == TokenType.NewLine))
            {
                skipped.Add(this.TokenStream.Next());
            }

            return true;
        }

        /// <summary>
        /// Comments out a line-wide comment, if any.
        /// </summary>
        /// <returns>Boolean value</returns>
        private bool CommentOutLineComment()
        {
            if ((this.TokenStream.Peek().Type != TokenType.CommentLine) &&
                (this.TokenStream.Peek().Type != TokenType.Region))
            {
                return false;
            }

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.NewLine)
            {
                this.TokenStream.Consume();
            }

            return true;
        }

        /// <summary>
        /// Comments out a multi-line comment, if any.
        /// </summary>
        /// <returns>Boolean value</returns>
        private bool CommentOutMultiLineComment()
        {
            if (this.TokenStream.Peek().Type != TokenType.CommentStart)
            {
                return false;
            }

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.CommentEnd)
            {
                this.TokenStream.Consume();
            }

            this.TokenStream.Consume();

            return true;
        }

        #endregion
    }
}
