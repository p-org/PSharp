//-----------------------------------------------------------------------
// <copyright file="ControlFlowGraph.cs">
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
