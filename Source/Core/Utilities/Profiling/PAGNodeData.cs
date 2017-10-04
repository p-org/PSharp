using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// Data that a node in a Program Activity Graph (PAG) holds
    /// </summary>
    public class PAGNodeData
    {
        /// <summary>
        /// The name of the node
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The time at which this node was created,
        /// according to the clock of the machine
        /// that created it
        /// </summary>
        public long LocalElapsedTime { get; private set; }

        /// <summary>
        /// The longest time on the critical path that must
        /// elapse before this node is created
        /// </summary>
        public long LongestElapsedTime { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="localElapsedTime"></param>
        /// <param name="longestElapsedTime"></param>
        public PAGNodeData(string name, long localElapsedTime, long longestElapsedTime)
        {
            Name = name;
            LocalElapsedTime = localElapsedTime;
            LongestElapsedTime = longestElapsedTime;
        }

        /// <summary>
        /// A string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0}[{1}]", Name, LongestElapsedTime);
        }

    }
}
