using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PLinearTopology
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterMachine(typeof(GodMachine));
            Runtime.RegisterMachine(typeof(Clock));
            Runtime.RegisterMachine(typeof(PortMachine));
            
            Runtime.Start();
        }
    }
}
