using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// RsmId implementation for Bug Finding
    /// </summary>
    internal class BugFindingRsmId : IRsmId
    {
        /// <summary>
        /// Machine representing the RSM
        /// </summary>
        public MachineId Mid;

        /// <summary>
        /// Name
        /// </summary>
        public string Name => Mid.Name;

        /// <summary>
        /// Partition hosting the RSM
        /// </summary>
        public string PartitionName { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="partitionName"></param>
        public BugFindingRsmId(MachineId mid, string partitionName)
        {
            this.Mid = mid;
            this.PartitionName = partitionName;
        }

        public int CompareTo(IRsmId other)
        {
            return Mid.Value.CompareTo((other as BugFindingRsmId).Mid.Value);
        }

        public bool Equals(IRsmId other)
        {
            return Mid.Equals((other as BugFindingRsmId).Mid);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

