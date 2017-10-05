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
        /// The time spent idling at this node
        /// </summary>
        public long IdleTime { get; private set; }

        /// <summary>
        /// The longest time on the critical path that must
        /// elapse before this node is created
        /// </summary>
        public long LongestElapsedTime { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="idleTime"></param>
        /// <param name="longestElapsedTime"></param>
        public PAGNodeData(string name, long idleTime, long longestElapsedTime)
        {
            Name = name;
            IdleTime = idleTime;
            LongestElapsedTime = longestElapsedTime;
        }

        /// <summary>
        /// A string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IdleTime == 0)
                return String.Format("{0}[{1}]", Name, LongestElapsedTime);
            else
                return String.Format("{0}[{1}/{2}]", Name, IdleTime, LongestElapsedTime);
        }

    }
}
