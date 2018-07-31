using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric
{
    [DataContract]
    internal abstract class OutboxElement
    {
    }

    [DataContract]
    internal class CreationRequest : OutboxElement
    {
        [DataMember]
        public string MachineType { get; private set; }
        [DataMember]
        public MachineId TargetMachine { get; private set; }
        [DataMember]
        public Event Payload { get; private set; }

        public CreationRequest(string machineType, MachineId target, Event ev)
        {
            this.MachineType = machineType;
            this.TargetMachine = target;
            this.Payload = ev;
        }
    }

    [DataContract]
    internal class MessageRequest : OutboxElement
    {
        [DataMember]
        public MachineId TargetMachine { get; private set; }
        [DataMember]
        public Event Payload { get; private set; }

        public MessageRequest(MachineId target, Event ev)
        {
            this.TargetMachine = target;
            this.Payload = ev;
        }
    }
}
