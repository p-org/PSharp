using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PLinearTopology
{
    public class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
            Console.ReadLine();
        }

        [EntryPoint]
        public static void Execute()
        {
            Runtime.CreateMachine<GodMachine>();
        }
    }
}
