using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Timers
{
    /// <summary>
    /// Timer configuration
    /// </summary>
    [DataContract]
    public class ReliableTimerConfig
    {
        /// <summary>
        /// Timeout value (ms)
        /// </summary>
        [DataMember]
        public readonly int Period;

        /// <summary>
        /// Name of the timer
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Initializes a timer configuration
        /// </summary>
        /// <param name="name">Name of the timer</param>
        /// <param name="period">Period (ms)</param>
        public ReliableTimerConfig(string name, int period)
        {
            this.Name = name;
            this.Period = period;
        }
    }
}
