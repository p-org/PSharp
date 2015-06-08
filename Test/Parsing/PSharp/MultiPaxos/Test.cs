using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
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
            Runtime.CreateMachine<GodMachine>();
            Runtime.WaitMachines();
        }
    }
}
