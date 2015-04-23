using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace POpenWSN
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterNewEvent(typeof(newSlot));
            Runtime.RegisterNewEvent(typeof(endSlot));
            Runtime.RegisterNewEvent(typeof(Local));
            Runtime.RegisterNewEvent(typeof(TxDone));
            Runtime.RegisterNewEvent(typeof(Tx));
            Runtime.RegisterNewEvent(typeof(Rx));
            Runtime.RegisterNewEvent(typeof(Sleep));
            Runtime.RegisterNewEvent(typeof(Data));
            Runtime.RegisterNewEvent(typeof(Ack));
            Runtime.RegisterNewEvent(typeof(Initialize));

            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(OpenWSN_Mote));
            Runtime.RegisterNewMachine(typeof(SlotTimerMachine));

            Runtime.Options.Verbose = true;
            Runtime.Start();
        }
    }
}
