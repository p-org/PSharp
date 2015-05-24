using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PLinearTopology
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterNewEvent(typeof(Announce));
            Runtime.RegisterNewEvent(typeof(Initialise));
            Runtime.RegisterNewEvent(typeof(PowerUp));
            Runtime.RegisterNewEvent(typeof(Local));
            Runtime.RegisterNewEvent(typeof(StateDecisionEvent));
            Runtime.RegisterNewEvent(typeof(Ack));
            Runtime.RegisterNewEvent(typeof(ErBest));
            Runtime.RegisterNewEvent(typeof(UpdateParentGM));
            Runtime.RegisterNewEvent(typeof(goMaster));
            Runtime.RegisterNewEvent(typeof(goSlave));
            Runtime.RegisterNewEvent(typeof(goPassive));
            Runtime.RegisterNewEvent(typeof(doneStateChange));

            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(Clock));
            Runtime.RegisterNewMachine(typeof(PortMachine));
            
            Runtime.Start();
        }
    }
}
