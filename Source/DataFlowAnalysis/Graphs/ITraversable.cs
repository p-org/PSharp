// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for a traversable node.
    /// </summary>
    public interface ITraversable<T>
        where T : INode
    {
        /// <summary>
        /// Set of the immediate successors.
        /// </summary>
        ISet<T> ISuccessors { get; }

        /// <summary>
        /// Set of the immediate predecessors.
        /// </summary>
        ISet<T> IPredecessors { get; }

        /// <summary>
        /// Returns true if the node is a successor
        /// of the specified node.
        /// </summary>
        bool IsSuccessorOf(T node);

        /// <summary>
        /// Returns true if the node is a predecessor
        /// of the specified node.
        /// </summary>
        bool IsPredecessorOf(T node);
    }
}
