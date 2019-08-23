// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface for a node.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Checks if the node contains the specified item.
        /// </summary>
        bool Contains<Item>(Item item);

        /// <summary>
        /// Checks if the node has no contents.
        /// </summary>
        bool IsEmpty();
    }
}
