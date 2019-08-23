// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for performing data-flow analysis.
    /// </summary>
    public interface IDataFlowAnalysis
    {
        /// <summary>
        /// Checks if the target symbol flows from the entry of the method.
        /// </summary>
        bool FlowsFromMethodEntry(ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the target symbol flows from the parameter list.
        /// </summary>
        bool FlowsFromParameterList(ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the target symbol flows from the parameter symbol.
        /// </summary>
        bool FlowsFromParameter(IParameterSymbol paramSymbol, ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        bool FlowsIntoSymbol(ISymbol symbol, ISymbol targetSymbol, Statement statement, Statement targetStatement);
    }
}
