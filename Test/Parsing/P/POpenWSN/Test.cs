using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace POpenWSN
{
    public class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
        }

        [EntryPoint]
        public static void Execute()
        {
            Runtime.RegisterMachine(typeof(GodMachine));
            Runtime.RegisterMachine(typeof(OpenWSN_Mote));
            Runtime.RegisterMachine(typeof(SlotTimerMachine));

            Runtime.Start();
        }
    }
}
