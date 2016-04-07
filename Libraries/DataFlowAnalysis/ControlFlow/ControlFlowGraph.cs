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
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// A control-flow graph.
    /// </summary>
    public class ControlFlowGraph
    {
        #region fields

        /// <summary>
        /// The unique id of the control-flow graph.
        /// </summary>
        public int Id;

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// Handle to the summary of the method which owns this node.
        /// </summary>
        private MethodSummary Summary;

        /// <summary>
        /// The entry node of the control-flow graph.
        /// </summary>
        public CFGNode EntryNode;

        /// <summary>
        /// Set of all exit nodes in the control-flow graph
        /// of the method of this summary.
        /// </summary>
        public HashSet<CFGNode> ExitNodes;

        /// <summary>
        /// Set of nodes in the control-flow graph.
        /// </summary>
        internal HashSet<CFGNode> Nodes;

        /// <summary>
        /// A counter for creating unique ids.
        /// </summary>
        private static int IdCounter;

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ControlFlowGraph()
        {
            ControlFlowGraph.IdCounter = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        private ControlFlowGraph(AnalysisContext context, MethodSummary summary)
        {
            this.Id = ControlFlowGraph.IdCounter++;
            this.AnalysisContext = context;
            this.Summary = summary;

            this.ExitNodes = new HashSet<CFGNode>();
            this.Nodes = new HashSet<CFGNode>();

            this.EntryNode = new CFGNode(this);
            this.EntryNode.Construct(summary.Method);

            this.MergeEmptyNodes();
            this.ExitNodes = this.EntryNode.GetExitNodes();
        }

        /// <summary>
        /// Creates a new control-flow graph.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>CFGNode</returns>
        internal static ControlFlowGraph Create(AnalysisContext context, MethodSummary summary)
        {
            return new ControlFlowGraph(context, summary);
        }

        #endregion

        #region public API

        /// <summary>
        /// Returns the method summary that contains this
        /// control-flow graph.
        /// </summary>
        /// <returns>MethodSummary</returns>
        public MethodSummary GetMethodSummary()
        {
            return this.Summary;
        }

        /// <summary>
        /// Adds an edge from the specified node to the target node.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <param name="successor">CFGNode</param>
        public void AddEdge(CFGNode fromNode, CFGNode toNode)
        {
            fromNode.ISuccessors.Add(toNode);
            toNode.IPredecessors.Add(fromNode);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Merges empty nodes.
        /// </summary>
        private void MergeEmptyNodes()
        {
            foreach (var node in this.Nodes)
            {
                if (node.Statements.Count == 0 &&
                    node.IPredecessors.Count > 0 &&
                    node.ISuccessors.Count == 1)
                {
                    foreach (var predecessor in node.IPredecessors)
                    {
                        predecessor.ISuccessors.Remove(node);
                        predecessor.ISuccessors.UnionWith(node.ISuccessors);
                    }

                    foreach (var successor in node.ISuccessors)
                    {
                        successor.IPredecessors.Remove(node);
                        successor.IPredecessors.UnionWith(node.IPredecessors);
                    }
                }
            }
        }

        #endregion
    }
}
