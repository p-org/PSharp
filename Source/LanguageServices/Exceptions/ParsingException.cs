// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        public ParsingException(string[] errors, string[] warnings)
            : base(string.Empty)
        {
            this.Errors = errors;
            this.Warnings = warnings;
            this.ExpectedTokenTypes = Array.Empty<TokenType>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        public ParsingException(string message, Token failingToken, params TokenType[] expectedTokensTypes)
            : this(message, null, failingToken, expectedTokensTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        public ParsingException(string message, Exception innerEx, Token failingToken, params TokenType[] expectedTokenTypes)
            : base(message, innerEx)
        {
            this.Errors = Array.Empty<string>();
            this.Warnings = Array.Empty<string>();
            this.FailingToken = failingToken;
            this.ExpectedTokenTypes = expectedTokenTypes;
        }
    }
}
