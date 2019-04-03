// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a generic graph.
    /// </summary>
    internal class Graph<T> : IGraph<T>
        where T : INode, ITraversable<T>
    {
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
        private static int IdCounter = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Graph{T}"/> class.
        /// </summary>
        protected Graph()
        {
            this.Id = IdCounter++;
            this.Nodes = new HashSet<T>();
        }

        /// <summary>
        /// Checks if the node is a successor of the specified node.
        /// </summary>
        public bool IsSuccessorOf(T successor, T node)
        {
            return this.IsSuccessorOf(successor, node, new HashSet<T>());
        }

        /// <summary>
        /// Checks if the node is a predecessor of the specified node.
        /// </summary>
        public bool IsPredecessorOf(T predecessor, T node)
        {
            return this.IsPredecessorOf(predecessor, node, new HashSet<T>());
        }

        /// <summary>
        /// Checks for the node that contains the specified item.
        /// </summary>
        public bool TryGetNodeContaining<Item>(Item item, out T result)
        {
            result = default;

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

        /// <summary>
        /// Checks if the node is a successor of the specified node.
        /// </summary>
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

        /// <summary>
        /// Pretty prints the graph.
        /// </summary>
        protected virtual void PrettyPrint(T currentNode, ISet<T> visited)
        {
        }
    }
}
