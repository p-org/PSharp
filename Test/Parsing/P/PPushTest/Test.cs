using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PPushTest
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(ePing));
            Runtime.RegisterNewEvent(typeof(ePong));

            Runtime.RegisterNewMachine(typeof(Ping));
            Runtime.RegisterNewMachine(typeof(Pong));
            
            Runtime.Start();
        }
    }
}
