using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;

namespace PingPong
{
    class PingEvent : RsmInitEvent
    {
        
    }

    class PongEvent : RsmInitEvent
    {
        public IRsmId PingMachineId;

        public PongEvent(IRsmId pingMachine)
        {
            this.PingMachineId = pingMachine;
        }
    }
}
