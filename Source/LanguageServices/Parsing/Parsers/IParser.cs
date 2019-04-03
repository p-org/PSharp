﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// Interface for a parser.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Returns a P# program.
        /// </summary>
        /// <param name="tokens">List of tokens.</param>
        IPSharpProgram ParseTokens(List<Token> tokens);

        /// <summary>
        /// Returns the expected token types at the end of parsing.
        /// </summary>
        /// <returns>The expected token types.</returns>
        TokenType[] GetExpectedTokenTypes();
    }
}
