using System;
using Microsoft.PSharp;

namespace OperationsExample
{
    class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(GodMachine));
        }
    }
}
