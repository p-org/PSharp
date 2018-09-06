// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface of a generic analysis pass.
    /// </summary>
    public interface IAnalysisPass
    {
        #region methods

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        void Run();

        #endregion
        
    }
}
