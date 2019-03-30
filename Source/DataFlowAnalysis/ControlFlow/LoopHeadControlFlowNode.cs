// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// A loop head control-flow graph node.
    /// </summary>
    internal class LoopHeadControlFlowNode : ControlFlowNode
    {
        /// <summary>
        /// The node after exiting the loop.
        /// </summary>
        public ControlFlowNode LoopExitNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopHeadControlFlowNode"/> class.
        /// </summary>
        internal LoopHeadControlFlowNode(IGraph<IControlFlowNode> cfg, MethodSummary summary, ControlFlowNode loopExitNode)
            : base(cfg, summary)
        {
            this.LoopExitNode = loopExitNode;
        }
    }
}
