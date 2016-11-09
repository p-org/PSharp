using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChainReplication
{
    public class ChessTest
    {
        public static bool Run()
        {
            var config = Microsoft.PSharp.Utilities.Configuration.Create();
            config.EnableMonitorsInProduction = true;

            var runtime = PSharpRuntime.Create(config);
            runtime.RegisterMonitor(typeof(InvariantMonitor));
            runtime.RegisterMonitor(typeof(ServerResponseSeqMonitor));

            Test.Execute(runtime);

            runtime.Wait();
            return true;
        }
    }

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
