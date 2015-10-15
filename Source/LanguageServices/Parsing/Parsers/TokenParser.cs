//-----------------------------------------------------------------------
// <copyright file="TokenParser.cs">
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

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// An abstract token parser.
    /// </summary>
    public abstract class TokenParser : BaseParser, IParser
    {
        #region fields

        /// <summary>
        /// List of original tokens.
        /// </summary>
        protected List<Token> OriginalTokens;

        /// <summary>
        /// The token stream.
        /// </summary>
        protected TokenStream TokenStream;

        /// <summary>
        /// List of expected token types at end of parsing.
        /// </summary>
        protected List<TokenType> ExpectedTokenTypes;

        /// <summary>
        /// The error log.
        /// </summary>
        protected string ErrorLog;

        #endregion

        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TokenParser()
            : base()
        {
            this.ErrorLog = "";
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="exitAtError">Exits at error</param>
        internal TokenParser(PSharpProject project, SyntaxTree tree, bool exitAtError)
            : base(project, tree, exitAtError)
        {
            this.ErrorLog = "";
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
                this.ParseTokens();
            }
            catch (ParsingException ex)
            {
                this.ErrorLog = ex.Message;
                this.ReportParsingError();
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

        /// <summary>
        /// Returns the parsing error log.
        /// </summary>
        /// <returns>Parsing error log</returns>
        public string GetParsingErrorLog()
        {
            return this.ErrorLog;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the tokens.
        /// </summary>
        protected abstract void ParseTokens();

        /// <summary>
        /// Reports a parsing error. Only works if the parser is
        /// running internally.
        /// </summary>
        protected void ReportParsingError()
        {
            if (!this.IsRunningInternally || this.ErrorLog.Length == 0)
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

            this.ErrorLog += "\nIn " + this.SyntaxTree.FilePath + " (line " + errorToken.TextUnit.Line + "):\n";

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
                this.ErrorLog += errorLine[idx].TextUnit.Text;
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Equals(errorToken) && errorIndex == this.TokenStream.Index)
                {
                    this.ErrorLog += new StringBuilder().Append('~', errorLine[idx].TextUnit.Text.Length);
                    break;
                }
                else
                {
                    this.ErrorLog += new StringBuilder().Append(' ', errorLine[idx].TextUnit.Text.Length);
                }
            }

            if (errorIndex != this.TokenStream.Index)
            {
                this.ErrorLog += "^";
            }

            ErrorReporter.ReportAndExit(this.ErrorLog);
        }

        #endregion
    }
}
