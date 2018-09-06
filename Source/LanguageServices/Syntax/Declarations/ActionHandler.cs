// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Anonymous action handler syntax node.
    /// </summary>
    internal class AnonymousActionHandler
    {
        #region fields

        /// <summary>
        /// The block containing the handler statements.
        /// </summary>
        internal readonly BlockSyntax BlockSyntax;

        /// <summary>
        /// Indicates whether the generated method should be 'async Task'.
        /// </summary>
        internal readonly bool IsAsync;

        #endregion

        #region internal API

        internal AnonymousActionHandler(BlockSyntax blockSyntax, bool isAsync)
        {
            this.BlockSyntax = blockSyntax;
            this.IsAsync = isAsync;
        }
        
        #endregion
    }
}
