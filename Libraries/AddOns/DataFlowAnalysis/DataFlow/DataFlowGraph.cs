//-----------------------------------------------------------------------
// <copyright file="DataFlowGraph.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// A data-flow graph.
    /// </summary>
    internal class DataFlowGraph : Graph<IDataFlowNode>, IDataFlowAnalysis
    {
        #region constructors

        /// <summary>
        /// Method summary that contains this graph.
        /// </summary>
        private MethodSummary Summary;

        /// <summary>
        /// The semantic model.
        /// </summary>
        private SemanticModel SemanticModel;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        internal DataFlowGraph(MethodSummary summary)
            : base()
        {
            this.Summary = summary;
            this.SemanticModel = summary.SemanticModel;

            base.EntryNode = DataFlowNode.Create(this, summary);
            base.MergeEmptyNodes();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Checks if the target symbol flows from the entry of the method.
        /// </summary>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
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
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        public bool FlowsFromParameterList(ISymbol targetSymbol, Statement targetStatement)
        {
            foreach (var param in targetStatement.Summary.Method.ParameterList.Parameters)
            {
                IParameterSymbol paramSymbol = this.SemanticModel.GetDeclaredSymbol(param);
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
        /// <param name="paramSymbol">Parameter Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        public bool FlowsFromParameter(IParameterSymbol paramSymbol, ISymbol targetSymbol,
            Statement targetStatement)
        {
            return this.FlowsIntoSymbol(paramSymbol, targetSymbol,
                targetStatement.Summary.DataFlowGraph.EntryNode.Statement,
                targetStatement);
        }

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        public bool FlowsIntoSymbol(ISymbol symbol, ISymbol targetSymbol,
            Statement statement, Statement targetStatement)
        {
            IDataFlowNode node;
            IDataFlowNode targetNode;
            if (!statement.Summary.DataFlowGraph.TryGetNodeContaining(statement, out node) ||
                !targetStatement.Summary.DataFlowGraph.TryGetNodeContaining(targetStatement, out targetNode) ||
                !this.Nodes.Contains(node) || !this.Nodes.Contains(targetNode))
            {
                return false;
            }
            
            return this.FlowsIntoSymbol(symbol, targetSymbol, node, targetNode);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        /// <param name="fromSymbol">From ISymbol</param>
        /// <param name="toSymbol">To ISymbol</param>
        /// <param name="fromNode">From IDataFlowNode</param>
        /// <param name="toNode">To IDataFlowNode</param>
        /// <returns>Boolean</returns>
        private bool FlowsIntoSymbol(ISymbol fromSymbol, ISymbol toSymbol,
            IDataFlowNode fromNode, IDataFlowNode toNode)
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
                    if (this.IsDefinitionReachingNodeInCycle(definition, fromNode, toNode))
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
        /// <param name="definition">SymbolDefinition</param>
        /// <param name="fromNode">IDataFlowNode</param>
        /// <param name="toNode">IDataFlowNode</param>
        /// <returns>Boolean</returns>
        private bool IsDefinitionReachingNodeInCycle(SymbolDefinition definition,
            IDataFlowNode fromNode, IDataFlowNode toNode)
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


        #endregion

        #region printing methods

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        /// <param name="currentNode">Current node</param>
        /// <param name="visited">Set of visited nodes</param>
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

        #endregion
    }
}
