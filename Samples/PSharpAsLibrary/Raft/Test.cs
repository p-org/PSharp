using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    public class ChessTest
    {
        public static bool Run()
        {
            var config = Microsoft.PSharp.Utilities.Configuration.Create();
            config.EnableMonitorsInProduction = true;

            var runtime = PSharpRuntime.Create(config);
            runtime.RegisterMonitor(typeof(SafetyMonitor));

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
            runtime.Wait();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(ClusterManager));
        }
    }
}
