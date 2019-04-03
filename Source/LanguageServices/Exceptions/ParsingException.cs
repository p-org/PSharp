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
        internal List<string> Errors;

        /// <summary>
        /// List of warnings.
        /// </summary>
        internal List<string> Warnings;

        /// <summary>
        /// The expected tokens.
        /// </summary>
        internal TokenType[] ExpectedTokenTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        public ParsingException(List<string> errors, List<string> warnings)
            : base(string.Empty)
        {
            this.Errors = errors;
            this.Warnings = warnings;
            this.ExpectedTokenTypes = Array.Empty<TokenType>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        public ParsingException(string message, params TokenType[] expectedTokensTypes)
            : this(message, null, expectedTokensTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingException"/> class.
        /// </summary>
        public ParsingException(string message, Exception innerEx, params TokenType[] expectedTokensTypes)
            : base(message, innerEx)
        {
            this.Errors = new List<string>();
            this.Warnings = new List<string>();
            this.ExpectedTokenTypes = expectedTokensTypes.ToArray();
        }
    }
}
