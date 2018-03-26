using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// Generator for unique RsmId
    /// </summary>
    internal class ServiceFabricRsmIdFactory
    {
        /// <summary>
        /// The counter
        /// </summary>
        long Counter;

        /// <summary>
        /// Partition name
        /// </summary>
        string PartitionName;

        /// <summary>
        /// Initializes the counter
        /// </summary>
        /// <param name="startingValue">Starting value</param>
        internal ServiceFabricRsmIdFactory(long startingValue, string partitionName)
        {
            Counter = startingValue;
            PartitionName = partitionName;
        }

        /// <summary>
        /// Creates a new unique Id
        /// </summary>
        /// <param name="name">Name attached to the Id</param>
        /// <returns></returns>
        internal ServiceFabricRsmId Generate(string name)
        {
            return new ServiceFabricRsmId(Counter, name, PartitionName);
        }

    }
}
