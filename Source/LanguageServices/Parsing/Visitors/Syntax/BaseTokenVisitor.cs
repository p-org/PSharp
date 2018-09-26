// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// An abstract P# token parsing visitor.
    /// </summary>
    internal abstract class BaseTokenVisitor
    {
        /// <summary>
        /// The token stream to visit.
        /// </summary>
        protected TokenStream TokenStream;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public BaseTokenVisitor(TokenStream tokenStream)
        {
            this.TokenStream = tokenStream;
        }
    }
}
