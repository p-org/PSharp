//-----------------------------------------------------------------------
// <copyright file="TopLevelRewriter.cs">
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
using System.Text.RegularExpressions;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# top level declaration rewriter.
    /// </summary>
    internal class TopLevelRewriter : BaseRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public TopLevelRewriter(List<Token> tokens)
            : base(tokens)
        {
            
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }

            var token = base.Tokens[base.Index];
            if (token.Type == TokenType.Machine)
            {
                this.RewriteMachineDeclaration();
            }

            base.Index++;
            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the machine declaration.
        /// </summary>
        private void RewriteMachineDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.Class);
            base.Index++;

            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                base.ReportParsingFailure();
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("Machine"));
            }
            else if (base.Tokens[base.Index].Type != TokenType.Doublecolon)
            {
                base.ReportParsingFailure();
            }
        }

        #endregion
    }
}
