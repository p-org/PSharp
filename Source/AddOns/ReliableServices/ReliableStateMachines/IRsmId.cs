using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// Unique identifier for an RSM
    /// </summary>
    public interface IRsmId : IComparable
    {
        /// <summary>
        /// Name of the identifier
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Partition where the RSM lives
        /// </summary>
        string PartitionName { get; }
    }
}
