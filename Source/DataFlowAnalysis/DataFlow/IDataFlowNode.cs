using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for a data-flow graph node.
    /// </summary>
    public interface IDataFlowNode : INode, ITraversable<IDataFlowNode>
    {
        /// <summary>
        /// Statement contained in the node.
        /// </summary>
        Statement Statement { get; }

        /// <summary>
        /// Graph that contains this node.
        /// </summary>
        IGraph<IDataFlowNode> Graph { get; }

        /// <summary>
        /// Control-flow graph node that contains this node.
        /// </summary>
        IControlFlowNode ControlFlowNode { get; }

        /// <summary>
        /// Method summary that contains this node.
        /// </summary>
        MethodSummary Summary { get; }

        /// <summary>
        /// The data-flow information of this node.
        /// </summary>
        DataFlowInfo DataFlowInfo { get; }

        /// <summary>
        /// Map from call sites to cached method summaries.
        /// </summary>
        IDictionary<ISymbol, ISet<MethodSummary>> MethodSummaryCache { get; }

        /// <summary>
        /// Set of gives-up ownership syntax statements.
        /// </summary>
        ISet<ISymbol> GivesUpOwnershipMap { get; }
    }
}
