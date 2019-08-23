using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// A data-flow graph.
    /// </summary>
    internal class DataFlowGraph : Graph<IDataFlowNode>, IDataFlowAnalysis
    {
        /// <summary>
        /// Method summary that contains this graph.
        /// </summary>
        private readonly MethodSummary Summary;

        /// <summary>
        /// The semantic model.
        /// </summary>
        private readonly SemanticModel SemanticModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowGraph"/> class.
        /// </summary>
        internal DataFlowGraph(MethodSummary summary)
            : base()
        {
            this.Summary = summary;
            this.SemanticModel = summary.SemanticModel;

            this.EntryNode = DataFlowNode.Create(this, summary);
            this.MergeEmptyNodes();
        }

        /// <summary>
        /// Checks if the target symbol flows from the entry of the method.
        /// </summary>
        public bool FlowsFromMethodEntry(ISymbol targetSymbol, Statement targetStatement)
        {
            var entryNode = targetStatement.Summary.DataFlowGraph.EntryNode;
            foreach (var definition in entryNode.DataFlowInfo.GeneratedDefinitions)
            {
                if (this.FlowsIntoSymbol(definition.Symbol, targetSymbol,
                    targetStatement.Summary.DataFlowGraph.EntryNode.Statement,
                    targetStatement))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the target symbol flows from the parameter list.
        /// </summary>
        public bool FlowsFromParameterList(ISymbol targetSymbol, Statement targetStatement)
        {
            foreach (var param in targetStatement.Summary.Method.ParameterList.Parameters)
            {
                var paramSymbol = this.SemanticModel.GetDeclaredSymbol(param) as IParameterSymbol;
                if (this.FlowsIntoSymbol(paramSymbol, targetSymbol,
                    targetStatement.Summary.DataFlowGraph.EntryNode.Statement,
                    targetStatement))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the target symbol flows from the parameter symbol.
        /// </summary>
        public bool FlowsFromParameter(IParameterSymbol paramSymbol, ISymbol targetSymbol, Statement targetStatement)
        {
            return this.FlowsIntoSymbol(paramSymbol, targetSymbol,
                targetStatement.Summary.DataFlowGraph.EntryNode.Statement,
                targetStatement);
        }

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        public bool FlowsIntoSymbol(ISymbol symbol, ISymbol targetSymbol, Statement statement, Statement targetStatement)
        {
            if (!statement.Summary.DataFlowGraph.TryGetNodeContaining(statement, out IDataFlowNode node) ||
                !targetStatement.Summary.DataFlowGraph.TryGetNodeContaining(targetStatement, out IDataFlowNode targetNode) ||
                !this.Nodes.Contains(node) || !this.Nodes.Contains(targetNode))
            {
                return false;
            }

            return this.FlowsIntoSymbol(symbol, targetSymbol, node, targetNode);
        }

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        private bool FlowsIntoSymbol(ISymbol fromSymbol, ISymbol toSymbol, IDataFlowNode fromNode, IDataFlowNode toNode)
        {
            var fromAliasDefinitions = fromNode.DataFlowInfo.ResolveOutputAliases(fromSymbol);
            var toAliasDefinitions = toNode.DataFlowInfo.ResolveOutputAliases(toSymbol);
            var commonDefinitions = fromAliasDefinitions.Intersect(toAliasDefinitions);

            if (!commonDefinitions.Any())
            {
                return false;
            }

            if (fromNode.IsSuccessorOf(toNode) &&
                toNode.IsSuccessorOf(fromNode))
            {
                foreach (var definition in commonDefinitions)
                {
                    if (IsDefinitionReachingNodeInCycle(definition, fromNode, toNode))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the specified definition is alive in some
        /// path between the two specified nodes.
        /// </summary>
        private static bool IsDefinitionReachingNodeInCycle(SymbolDefinition definition, IDataFlowNode fromNode, IDataFlowNode toNode)
        {
            var queue = new Queue<IList<IDataFlowNode>>();
            queue.Enqueue(new List<IDataFlowNode> { fromNode });

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var node = path.Last();

                if (node.Equals(toNode) && path.Count > 1)
                {
                    bool isAlive = true;
                    foreach (var visitedNode in path)
                    {
                        var generatedDefinition = visitedNode.DataFlowInfo.
                            GetGeneratedDefinitionOfSymbol(definition.Symbol);
                        if (generatedDefinition != null &&
                            generatedDefinition.Equals(definition))
                        {
                            isAlive = false;
                            break;
                        }
                    }

                    if (isAlive)
                    {
                        return true;
                    }
                }

                foreach (var successor in node.ISuccessors.Where(
                    n => !path.Skip(1).Contains(n)))
                {
                    var nextPath = new List<IDataFlowNode>(path);
                    nextPath.Add(successor);
                    queue.Enqueue(nextPath);
                }
            }

            return false;
        }

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        protected override void PrettyPrint(IDataFlowNode currentNode, ISet<IDataFlowNode> visited)
        {
            if (visited.Contains(currentNode))
            {
                return;
            }

            visited.Add(currentNode);

            Console.WriteLine("... |");
            Console.WriteLine("... | . Node id '{0}'", currentNode);

            if (currentNode.Statement != null)
            {
                Console.WriteLine("... | ... {0}", currentNode.Statement.SyntaxNode);
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
