//-----------------------------------------------------------------------
// <copyright file="ParsingException.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// Exception thrown during parsing.
    /// </summary>
    internal class ParsingException : Exception
    {
        /// <summary>
        /// List of errors.
        /// </summary>
        internal string[] Errors { get; private set; }

        /// <summary>
        /// List of warnings.
        /// </summary>
        internal string[] Warnings { get; private set; }

        /// <summary>
        /// The token that triggered the exception.
        /// </summary>
        internal Token FailingToken { get; private set; }

        /// <summary>
        /// The expected tokens.
        /// </summary>
        internal TokenType[] ExpectedTokenTypes { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="errors">List of errors</param>
        /// <param name="warnings">List of warnings</param>
        public ParsingException(string[] errors, string[] warnings)
            : base("")
        {
            this.Errors = errors;
            this.Warnings = warnings;
            this.ExpectedTokenTypes = Array.Empty<TokenType>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="failingToken">The token at the point of parsing failure</param>
        /// <param name="expectedTokenTypes">Expected token types</param>
        public ParsingException(string message, Token failingToken, params TokenType[] expectedTokenTypes)
            : base(message)
        {
            this.Errors = Array.Empty<string>();
            this.Warnings = Array.Empty<string>();
            this.FailingToken = failingToken;
            this.ExpectedTokenTypes = expectedTokenTypes;
        }
    }
}
