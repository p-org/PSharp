// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        /// <param name="message">Message</param>
        /// <param name="expectedTokensTypes">Expected token types</param>
        public ParsingException(string message, List<TokenType> expectedTokensTypes = null)
            : this(message, expectedTokensTypes ?? new List<TokenType> {}, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="expectedTokensTypes">Expected token types</param>
        /// <param name="innerEx">Inner Exception, if any</param>
        public ParsingException(string message, List<TokenType> expectedTokensTypes, Exception innerEx)
            : base(message, innerEx)
        {
            this.Errors = new List<string>();
            this.Warnings = new List<string>();
            this.ExpectedTokenTypes = expectedTokensTypes;
        }
    }
}
