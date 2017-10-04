using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// A node with a unique id
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// The unique id for this node
        /// </summary>
        long Id { get; }
    }
}
