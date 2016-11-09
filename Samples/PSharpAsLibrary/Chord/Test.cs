using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Chord
{
    public class ChessTest
    {
        public static bool Run()
        {
            var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);

            runtime.Wait();
            return true;
        }
    }

    /// <summary>
    /// How to run:
    /// 
    /// .\PSharpTester.exe /test:PSharp\Samples\PSharpAsLibrary\Binaries\Debug\Chord.dll /i:100 /max-steps:1000
    /// 
    /// Liveness bug found in:
    ///  
    /// Client.cs, line 53
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
            runtime.CreateMachine(typeof(ClusterManager));
        }
    }
}
