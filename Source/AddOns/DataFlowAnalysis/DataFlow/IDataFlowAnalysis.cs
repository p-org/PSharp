// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for performing data-flow analysis.
    /// </summary>
    public interface IDataFlowAnalysis
    {
        /// <summary>
        /// Checks if the target symbol flows from the entry of the method.
        /// </summary>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsFromMethodEntry(ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the target symbol flows from the parameter list.
        /// </summary>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsFromParameterList(ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the target symbol flows from the parameter symbol.
        /// </summary>
        /// <param name="paramSymbol">Parameter Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsFromParameter(IParameterSymbol paramSymbol, ISymbol targetSymbol,
            Statement targetStatement);

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsIntoSymbol(ISymbol symbol, ISymbol targetSymbol,
            Statement statement, Statement targetStatement);
    }
}
