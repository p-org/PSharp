//-----------------------------------------------------------------------
// <copyright file="Graph.cs">
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
    /// Class implementing a generic graph.
    /// </summary>
    internal class Graph<T> : IGraph<T> where T : INode, ITraversable<T>
    {
        #region fields

        /// <summary>
        /// The unique id of the graph.
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// The entry node of the graph.
        /// </summary>
        public T EntryNode { get; protected set; }

        /// <summary>
        /// Set of nodes in the graph.
        /// </summary>
        public ISet<T> Nodes { get; protected set; }

        /// <summary>
        /// A counter for creating unique ids.
        /// </summary>
        private static int IdCounter;

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Graph()
        {
            Graph<T>.IdCounter = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Graph()
        {
            this.Id = Graph<T>.IdCounter++;
            this.Nodes = new HashSet<T>();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Checks if the node is a successor of the specified node.
        /// </summary>
        /// <param name="successor">INode</param>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        public bool IsSuccessorOf(T successor, T node)
        {
            return this.IsSuccessorOf(successor, node, new HashSet<T>());
        }

        /// <summary>
        /// Checks if the node is a predecessor of the specified node.
        /// </summary>
        /// <param name="predecessor">INode</param>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        public bool IsPredecessorOf(T predecessor, T node)
        {
            return this.IsPredecessorOf(predecessor, node, new HashSet<T>());
        }

        /// <summary>
        /// Checks for the node that contains the specified item.
        /// </summary>
        /// <param name="item">Item</param>
        /// <param name="result">INode</param>
        /// <returns>Boolean</returns>
        public bool TryGetNodeContaining<Item>(Item item, out T result)
        {
            result = default(T);

            var queue = new Queue<T>();
            queue.Enqueue(this.EntryNode);

            var visited = new HashSet<T>();

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                visited.Add(node);

                if (node.Contains<Item>(item))
                {
                    result = node;
                    return true;
                }

                foreach (var successor in node.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    queue.Enqueue(successor);
                }
            }

            return false;
        }

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        public void PrettyPrint()
        {
            this.PrettyPrint(this.EntryNode, new HashSet<T>());
        }

        #endregion

        #region protected methods
        
        /// <summary>
        /// Merges empty nodes.
        /// </summary>
        protected void MergeEmptyNodes()
        {
            foreach (var node in this.Nodes)
            {
                if (node.IsEmpty() &&
                    node.IPredecessors.Count > 0 &&
                    node.ISuccessors.Count == 1)
                {
                    foreach (var predecessor in node.IPredecessors)
                    {
                        predecessor.ISuccessors.Remove(node);
                        foreach (var successor in node.ISuccessors)
                        {
                            predecessor.ISuccessors.Add(successor);
                        }
                    }

                    foreach (var successor in node.ISuccessors)
                    {
                        successor.IPredecessors.Remove(node);
                        foreach (var predecessor in node.IPredecessors)
                        {
                            successor.IPredecessors.Add(predecessor);
                        }
                    }
                }
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks if the node is a successor of the specified node.
        /// </summary>
        /// <param name="successor">IControlFlowNode</param>
        /// <param name="node">IControlFlowNode</param>
        /// <param name="visited">Already visited nodes</param>
        /// <returns>Boolean</returns>
        private bool IsSuccessorOf(T successor, T node, ISet<T> visited)
        {
            visited.Add(successor);

            if (successor.IPredecessors.Contains(node))
            {
                return true;
            }

            foreach (var predecessor in successor.IPredecessors.Where(v => !visited.Contains(v)))
            {
                if (this.IsSuccessorOf(predecessor, node, visited))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the node is a predecessor of the specified node.
        /// </summary>
        /// <param name="predecessor">IControlFlowNode</param>
        /// <param name="node">IControlFlowNode</param>
        /// <param name="visited">Already visited nodes</param>
        /// <returns>Boolean</returns>
        private bool IsPredecessorOf(T predecessor, T node, ISet<T> visited)
        {
            visited.Add(predecessor);

            if (predecessor.ISuccessors.Contains(node))
            {
                return true;
            }

            foreach (var successor in predecessor.ISuccessors.Where(v => !visited.Contains(v)))
            {
                if (this.IsPredecessorOf(successor, node, visited))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all exit nodes in the graph.
        /// </summary>
        /// <param name="currentNode">INode</param>
        /// <param name="visited">Already visited nodes</param>
        /// <returns>Set of exit nodes</returns>
        private ISet<T> GetExitNodes(T currentNode, ISet<T> visited)
        {
            visited.Add(currentNode);

            var exitNodes = new HashSet<T>();
            if (currentNode.ISuccessors.Count == 0)
            {
                exitNodes.Add(currentNode);
            }
            else
            {
                foreach (var successor in currentNode.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    var nodes = this.GetExitNodes(successor, visited);
                    foreach (var node in nodes)
                    {
                        exitNodes.Add(node);
                    }
                }
            }

            return exitNodes;
        }

        #endregion

        #region printing methods

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        /// <param name="currentNode">INode</param>
        /// <param name="visited">Set of visited nodes</param>
        protected virtual void PrettyPrint(T currentNode, ISet<T> visited) { }

        #endregion
    }
}
