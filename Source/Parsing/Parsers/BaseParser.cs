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
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Abstract parser.
    /// </summary>
    internal abstract class BaseParser
    {
        #region fields

        /// <summary>
        /// Lines of tokens.
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
        /// <param name="tokens">List of tokens</param>
        public BaseParser(List<Token> tokens)
        {
            this.Tokens = tokens;
            this.Index = 0;
            this.CurrentMachine = "";
            this.CurrentState = "";
        }

        /// <summary>
        /// Returns the parsed tokens.
        /// </summary>
        /// <returns>Parsed tokens</returns>
        public List<Token> GetParsedTokens()
        {
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
        /// Skips white space tokens.
        /// </summary>
        protected void SkipWhiteSpaceTokens()
        {
            while (this.Index < this.Tokens.Count &&
                (this.Tokens[this.Index].Type == TokenType.WhiteSpace ||
                this.Tokens[this.Index].Type == TokenType.NewLine))
            {
                this.Index++;
            }

            if (this.Index == this.Tokens.Count)
            {
                throw new ParsingException("parser: unexpected end of token list.");
            }
        }

        /// <summary>
        /// Erases white space tokens.
        /// </summary>
        protected void EraseWhiteSpaceTokens()
        {
            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type == TokenType.WhiteSpace)
            {
                this.Tokens.RemoveAt(this.Index);
            }

            if (this.Index == this.Tokens.Count)
            {
                throw new ParsingException("parser: unexpected end of token list.");
            }
        }

        #endregion
    }
}
