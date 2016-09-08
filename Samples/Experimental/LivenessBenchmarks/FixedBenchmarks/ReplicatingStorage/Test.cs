using System;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
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
