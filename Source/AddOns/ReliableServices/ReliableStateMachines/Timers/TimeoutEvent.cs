using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Timers
{
    /// <summary>
    /// Timeout event delivered by a reliable timer
    /// </summary>
    public class TimeoutEvent : Event
    {
        /// <summary>
        /// Name of the timer that fired
        /// </summary>
        public readonly string Name;

        public TimeoutEvent(string name)
        {
            this.Name = name;
        }
    }
}
