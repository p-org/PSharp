using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChainReplication
{
    public class Test
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(InvariantMonitor));
            runtime.RegisterMonitor(typeof(ServerResponseSeqMonitor));
            runtime.CreateMachine(typeof(Environment));
        }
    }
}
