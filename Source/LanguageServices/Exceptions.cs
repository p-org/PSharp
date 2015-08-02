//-----------------------------------------------------------------------
// <copyright file="Exceptions.cs">
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

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// Implements a parsing exception.
    /// </summary>
    internal class ParsingException : Exception
    {
        /// <summary>
        /// The expected tokens.
        /// </summary>
        internal List<TokenType> ExpectedTokenTypes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="expectedTokensTypes">Expected token types</param>
        public ParsingException(List<TokenType> expectedTokensTypes)
            : base("")
        {
            this.ExpectedTokenTypes = expectedTokensTypes;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="expectedTokensTypes">Expected token types</param>
        public ParsingException(string message, List<TokenType> expectedTokensTypes)
            : base(message)
        {
            this.ExpectedTokenTypes = expectedTokensTypes;
        }
    }

    /// <summary>
    /// Implements a rewriting exception.
    /// </summary>
    internal class RewritingException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        public RewritingException(string message)
            : base(message)
        {

        }
    }
}
