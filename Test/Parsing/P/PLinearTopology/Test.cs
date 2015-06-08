using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PLinearTopology
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.CreateMachine<GodMachine>();
            Runtime.WaitMachines();
        }
    }
}
