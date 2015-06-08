using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PGerman
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
            Runtime.CreateMachine<Host>();
            Runtime.WaitMachines();
        }
    }
}
