using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;

namespace PingPong
{
    [DataContract]
    public class PingEvent : Event
    {
        
    }

    [DataContract]
    public class PongEvent : Event
    {
        [DataMember]
        public MachineId PingMachineId;

        public PongEvent(MachineId pingMachine)
        {
            this.PingMachineId = pingMachine;
        }
    }
}
