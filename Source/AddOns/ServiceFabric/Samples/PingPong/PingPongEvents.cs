using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;

namespace PingPong
{
    class PingEvent : Event
    {
        
    }

    class PongEvent : Event
    {
        public MachineId PingMachineId;

        public PongEvent(MachineId pingMachine)
        {
            this.PingMachineId = pingMachine;
        }
    }
}
