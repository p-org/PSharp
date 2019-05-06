// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# token tag.
    /// </summary>
    internal class PSharpTokenTag : ITag
    {
        public TokenType Type { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">TokenType</param>
        public PSharpTokenTag(TokenType type) => this.Type = type;
    }
}
