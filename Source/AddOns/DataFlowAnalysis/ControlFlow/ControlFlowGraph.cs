// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// A control-flow graph.
    /// </summary>
    internal class ControlFlowGraph : Graph<IControlFlowNode>
    {
        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        internal ControlFlowGraph(MethodSummary summary)
            : base()
        {
            base.EntryNode = ControlFlowNode.Create(this, summary);
            base.MergeEmptyNodes();
        }

        #endregion

        #region printing methods

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        /// <param name="currentNode">Current node</param>
        /// <param name="visited">Set of visited nodes</param>
        protected override void PrettyPrint(IControlFlowNode currentNode, ISet<IControlFlowNode> visited)
        {
            if (visited.Contains(currentNode))
            {
                return;
            }

            visited.Add(currentNode);

            Console.WriteLine("... |");
            Console.WriteLine("... | . Node id '{0}'", currentNode);

            foreach (var node in currentNode.Statements)
            {
                Console.WriteLine("... | ... {0}", node.SyntaxNode);
            }

            string successors = "... | ..... successors:";
            foreach (var node in currentNode.ISuccessors)
            {
                successors += $" '{node}'";
            }

            if (currentNode.ISuccessors.Count == 0)
            {
                successors += " 'Exit'";
            }

            string predecessors = "... | ..... predecessors:";
            foreach (var node in currentNode.IPredecessors)
            {
                predecessors += $" '{node}'";
            }

            if (currentNode.IPredecessors.Count == 0)
            {
                predecessors += " 'Entry'";
            }

            Console.WriteLine(successors);
            Console.WriteLine(predecessors);

            foreach (var node in currentNode.ISuccessors)
            {
                this.PrettyPrint(node, visited);
            }
        }

        #endregion
    }
}
