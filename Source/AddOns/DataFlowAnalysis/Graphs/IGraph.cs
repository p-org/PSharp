// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface of a generic graph.
    /// </summary>
    public interface IGraph<T> where T : INode, ITraversable<T>
    {
        #region properties

        /// <summary>
        /// The entry node of the graph.
        /// </summary>
        T EntryNode { get; }

        /// <summary>
        /// Set of nodes in the graph.
        /// </summary>
        ISet<T> Nodes { get; }

        #endregion

        #region methods

        /// <summary>
        /// Checks if the node is a successor of the specified node.
        /// </summary>
        /// <param name="successor">INode</param>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        bool IsSuccessorOf(T successor, T node);

        /// <summary>
        /// Checks if the node is a predecessor of the specified node.
        /// </summary>
        /// <param name="predecessor">INode</param>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        bool IsPredecessorOf(T predecessor, T node);

        /// <summary>
        /// Checks for the node that contains the specified item.
        /// </summary>
        /// <param name="item">Item</param>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        bool TryGetNodeContaining<Item>(Item item, out T node);

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        void PrettyPrint();

        #endregion
    }
}
