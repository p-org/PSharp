// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for a node.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Checks if the node contains the specified item.
        /// </summary>
        bool Contains<Item>(Item item);

        /// <summary>
        /// Checks if the node has no contents.
        /// </summary>
        bool IsEmpty();
    }
}
