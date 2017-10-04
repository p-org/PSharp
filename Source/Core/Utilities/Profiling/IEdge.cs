using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// A directed edge
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public interface IEdge<TNode>
    {
        /// <summary>
        /// The source node
        /// </summary>
        TNode Source { get; }

        /// <summary>
        /// The target node
        /// </summary>
        TNode Target { get; }

    }
}
