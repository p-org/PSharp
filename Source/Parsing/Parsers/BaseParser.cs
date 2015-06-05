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
            this.TokenStream = new TokenStream(tokens);
            this.Program = this.CreateNewProgram();
            this.ExpectedTokenTypes = new List<TokenType>();

            try
            {
                this.ParseNextToken();
            }
            catch (ParsingException ex)
            {
                this.ReportParsingError(ex.Message);
                this.ExpectedTokenTypes = ex.ExpectedTokenTypes;
            }
            
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
            if (!this.IsRunningInternally || error.Length == 0)
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

            ErrorReporter.ReportAndExit(error);
        }

        #endregion
    }
}
