using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// A data-flow graph node.
    /// </summary>
    internal class DataFlowNode : IDataFlowNode
    {
        /// <summary>
        /// The unique id of the node.
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// Method summary that contains this node.
        /// </summary>
        public MethodSummary Summary { get; private set; }

        /// <summary>
        /// Graph that contains this node.
        /// </summary>
        public IGraph<IDataFlowNode> Graph { get; private set; }

        /// <summary>
        /// Control-flow graph node that contains this node.
        /// </summary>
        public IControlFlowNode ControlFlowNode { get; private set; }

        /// <summary>
        /// Statement contained in the node.
        /// </summary>
        public Statement Statement { get; private set; }

        /// <summary>
        /// Set of the immediate successors.
        /// </summary>
        public ISet<IDataFlowNode> ISuccessors { get; private set; }

        /// <summary>
        /// Set of the immediate predecessors.
        /// </summary>
        public ISet<IDataFlowNode> IPredecessors { get; private set; }

        /// <summary>
        /// The data-flow information of this node.
        /// </summary>
        public DataFlowInfo DataFlowInfo { get; private set; }

        /// <summary>
        /// Map from call sites to cached method summaries.
        /// </summary>
        public IDictionary<ISymbol, ISet<MethodSummary>> MethodSummaryCache { get; private set; }

        /// <summary>
        /// Set of gives-up ownership syntax statements.
        /// </summary>
        public ISet<ISymbol> GivesUpOwnershipMap { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowNode"/> class.
        /// </summary>
        internal DataFlowNode(IGraph<IDataFlowNode> dfg, IControlFlowNode cfgNode, MethodSummary summary)
        {
            this.Id = dfg.Nodes.Count;
            this.Graph = dfg;
            this.ControlFlowNode = cfgNode;
            this.Summary = summary;

            this.ISuccessors = new HashSet<IDataFlowNode>();
            this.IPredecessors = new HashSet<IDataFlowNode>();
            dfg.Nodes.Add(this);

            this.DataFlowInfo = new DataFlowInfo(this, summary.AnalysisContext);
            this.MethodSummaryCache = new Dictionary<ISymbol, ISet<MethodSummary>>();
            this.GivesUpOwnershipMap = new HashSet<ISymbol>();
        }

        /// <summary>
        /// Creates the control-flow graph nodes of the specified method summary.
        /// </summary>
        internal static IDataFlowNode Create(DataFlowGraph dfg, MethodSummary summary)
        {
            var entryNode = new DataFlowNode(dfg, summary.ControlFlowGraph.EntryNode, summary);
            entryNode.Construct(summary.ControlFlowGraph.EntryNode, null,
                new Dictionary<IControlFlowNode, DataFlowNode>());
            return entryNode;
        }

        /// <summary>
        /// Checks the node is a successor of the specified node.
        /// </summary>
        public bool IsSuccessorOf(IDataFlowNode node)
        {
            return this.Graph.IsSuccessorOf(this, node);
        }

        /// <summary>
        /// Checks the node is a predecessor of the specified node.
        /// </summary>
        public bool IsPredecessorOf(IDataFlowNode node)
        {
            return this.Graph.IsPredecessorOf(this, node);
        }

        /// <summary>
        /// Checks if the node contains the specified item.
        /// </summary>
        public bool Contains<T>(T item)
        {
            if (!(item is Statement))
            {
                return false;
            }

            var stmt = item as Statement;
            if (this.Statement is null ||
                !this.Statement.Equals(stmt))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the node is empty.
        /// </summary>
        public bool IsEmpty()
        {
            if (this.Statement is null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Constructs the data-flow graph node from the specified control-flow graph node.
        /// </summary>
        private void Construct(IControlFlowNode cfgNode, DataFlowNode previous, Dictionary<IControlFlowNode, DataFlowNode> visited)
        {
            visited.Add(cfgNode, this);

            if (cfgNode.Statements.Count > 0)
            {
                this.Statement = cfgNode.Statements[0];
            }

            previous = this;
            for (int idx = 1; idx < cfgNode.Statements.Count; idx++)
            {
                var nextNode = new DataFlowNode(this.Graph, cfgNode, this.Summary);
                nextNode.Statement = cfgNode.Statements[idx];

                previous.ISuccessors.Add(nextNode);
                nextNode.IPredecessors.Add(previous);
                previous = nextNode;
            }

            foreach (var successor in cfgNode.ISuccessors)
            {
                if (visited.ContainsKey(successor))
                {
                    previous.ISuccessors.Add(visited[successor]);
                    visited[successor].IPredecessors.Add(previous);
                    continue;
                }

                var nextNode = new DataFlowNode(this.Graph, successor, this.Summary);
                nextNode.Construct(successor, previous, visited);
                previous.ISuccessors.Add(nextNode);
                nextNode.IPredecessors.Add(previous);
            }
        }

        public override string ToString() => $"{this.Id}";
    }
}
