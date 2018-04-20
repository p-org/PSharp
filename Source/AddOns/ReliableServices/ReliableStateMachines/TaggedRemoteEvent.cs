using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.ReliableServices
{

    /// <summary>
    /// Tagged event for sending remote
    /// </summary>
    [DataContract]
    public class TaggedRemoteEvent : Event
    {
        /// <summary>
        /// Tag
        /// </summary>
        [DataMember]
        public int tag { get; private set; }

        /// <summary>
        /// Payload
        /// </summary>
        [DataMember]
        public Event ev { get; private set; }

        /// <summary>
        /// Sender
        /// </summary>
        [DataMember]
        public IRsmId mid { get; private set; }

        public TaggedRemoteEvent(IRsmId sender, Event ev, int tag)
        {
            this.tag = tag;
            this.ev = ev;
            this.mid = sender;
        }
    }
}