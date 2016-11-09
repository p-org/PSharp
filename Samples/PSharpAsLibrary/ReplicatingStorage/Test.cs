using System;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    public class ChessTest
    {
        public static bool Run()
        {
            var config = Microsoft.PSharp.Utilities.Configuration.Create();
            config.EnableMonitorsInProduction = true;

            var runtime = PSharpRuntime.Create(config);
            runtime.RegisterMonitor(typeof(LivenessMonitor));

            Test.Execute(runtime);

            runtime.Wait();
            return true;
        }
    }

    /// <summary>
    /// How to run:
    /// 
    /// .\PSharpTester.exe /test:PSharp\Samples\PSharpAsLibrary\Binaries\Debug\ReplicatingStorage.dll /i:100 /max-steps:1000
    /// 
    /// Liveness bug found in:
    ///  
    /// NodeManager.cs, line 181
    /// </summary>
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
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            runtime.CreateMachine(typeof(Environment));
        }
    }
}
