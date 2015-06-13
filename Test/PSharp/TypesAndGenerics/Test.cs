using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace TypesAndGenerics
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
            Runtime.CreateMachine<Server>();
        }
    }
}
