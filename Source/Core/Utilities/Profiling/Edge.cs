namespace Core.Utilities.Profiling
{
    /// <summary>
    /// Class representing an edge in a directed graph.
    /// The edge can be tagged with some data
    /// </summary>
    /// <typeparam name="TNode">The type of the nodes this edge connects.</typeparam>
    public class Edge<TNode> : IEdge<TNode>
    {
        private readonly TNode Source;

        private readonly TNode Target;

        /// <summary>
        /// Creates a new edge.
        /// </summary>
        /// <param name="source">The source node.</param>
        /// <param name="target">The target node.</param>
        public Edge(TNode source, TNode target)
        {
            Source = source;
            Target = target;
        }

        TNode IEdge<TNode>.Source => this.Source;

        TNode IEdge<TNode>.Target => this.Target;

        /// <summary>
        /// Return a string representation of this edge
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Source + "->" + this.Target;
        }
    }
}