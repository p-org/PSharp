using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace FailureDetector
{
    public class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(Driver));
        }
    }
}
