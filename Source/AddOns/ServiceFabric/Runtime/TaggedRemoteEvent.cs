using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric
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
        public MachineId mid { get; private set; }

        /// <summary>
        /// Creates a new TaggedRemoteEvent
        /// </summary>
        /// <param name="sender">Sender ID</param>
        /// <param name="ev">Event payload</param>
        /// <param name="tag">Tag numbers</param>
        public TaggedRemoteEvent(MachineId sender, Event ev, int tag)
        {
            this.tag = tag;
            this.ev = ev;
            this.mid = sender;
        }
    }
}
