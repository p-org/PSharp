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
        internal List<string> Errors;

        /// <summary>
        /// List of warnings.
        /// </summary>
        internal List<string> Warnings;

        /// <summary>
        /// The expected tokens.
        /// </summary>
        internal List<TokenType> ExpectedTokenTypes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="errors">List of errors</param>
        /// <param name="warnings">List of warnings</param>
        public ParsingException(List<string> errors, List<string> warnings)
            : base("")
        {
            this.Errors = errors;
            this.Warnings = warnings;
            this.ExpectedTokenTypes = new List<TokenType>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="expectedTokensTypes">Expected token types</param>
        public ParsingException(List<TokenType> expectedTokensTypes)
            : base("")
        {
            this.Errors = new List<string>();
            this.Warnings = new List<string>();
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
            this.Errors = new List<string>();
            this.Warnings = new List<string>();
            this.ExpectedTokenTypes = expectedTokensTypes;
        }
    }
}
