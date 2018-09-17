// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for a data-flow graph node.
    /// </summary>
    public interface IDataFlowNode : INode, ITraversable<IDataFlowNode>
    {
        #region properties

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

        #endregion
    }
}
