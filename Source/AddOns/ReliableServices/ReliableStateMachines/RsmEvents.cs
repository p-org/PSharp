using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// RSM Initialization event
    /// </summary>
    [DataContract]
    public class RsmInitEvent : Event
    {
        /// <summary>
        /// The host
        /// </summary>
        public RsmHost Host { get; internal set; }

        /// <summary>
        /// Initializes a new RsmInitEvent
        /// </summary>
        /// <param name="host"></param>
        public RsmInitEvent(RsmHost host)
        {
            this.Host = host;
        }
    }
}
