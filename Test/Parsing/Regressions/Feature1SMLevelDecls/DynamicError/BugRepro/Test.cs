using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Regression
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
            Runtime.RegisterMachine(typeof(Driver));
            Runtime.RegisterMachine(typeof(FailureDetector));
            Runtime.RegisterMachine(typeof(Node));
            Runtime.RegisterMachine(typeof(Timer));

            Runtime.Start();
        }
    }
}
