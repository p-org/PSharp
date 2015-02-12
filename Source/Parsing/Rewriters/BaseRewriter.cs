//-----------------------------------------------------------------------
// <copyright file="BaseRewriter.cs">
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
using System.IO;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Abstract rewriter.
    /// </summary>
    internal abstract class BaseRewriter
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

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public BaseRewriter(List<Token> tokens)
        {
            this.Tokens = tokens;
            this.Index = 0;
        }

        /// <summary>
        /// Returns the rewritten tokens.
        /// </summary>
        /// <returns>Rewritten tokens</returns>
        public List<Token> GetRewrittenTokens()
        {
            this.ParseNextToken();
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
                this.ReportParsingFailure();
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Reports a parting failure.
        /// </summary>
        protected void ReportParsingFailure()
        {
            ErrorReporter.ReportErrorAndExit("parser failed, please contact the developers.");
        }

        #endregion
    }
}
