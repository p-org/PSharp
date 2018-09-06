// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// A loop head control-flow graph node.
    /// </summary>
    internal class LoopHeadControlFlowNode : ControlFlowNode
    {
        #region fields

        /// <summary>
        /// The node after exiting the loop.
        /// </summary>
        public ControlFlowNode LoopExitNode;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cfg">ControlFlowGraph</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="loopExitNode">ControlFlowNode</param>
        internal LoopHeadControlFlowNode(IGraph<IControlFlowNode> cfg,
            MethodSummary summary, ControlFlowNode loopExitNode)
            : base(cfg, summary)
        {
            this.LoopExitNode = loopExitNode;
        }

        #endregion
    }
}
