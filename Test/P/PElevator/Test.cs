using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PElevator
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
            Runtime.CreateMachine(typeof(User));
        }
    }
}
