using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// A control-flow graph.
    /// </summary>
    internal class ControlFlowGraph : Graph<IControlFlowNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlFlowGraph"/> class.
        /// </summary>
        internal ControlFlowGraph(MethodSummary summary)
            : base()
        {
            this.EntryNode = ControlFlowNode.Create(this, summary);
            this.MergeEmptyNodes();
        }

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
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
    }
}
