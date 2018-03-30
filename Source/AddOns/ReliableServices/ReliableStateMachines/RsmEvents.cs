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
        internal RsmHost Host { get; set; }

        /// <summary>
        /// Initializes a new RsmInitEvent
        /// </summary>
        public RsmInitEvent()
        {
            this.Host = null;
        }

        /// <summary>
        /// Initializes a new RsmInitEvent
        /// </summary>
        internal RsmInitEvent(RsmHost host)
        {
            this.Host = host;
        }
    }
}
