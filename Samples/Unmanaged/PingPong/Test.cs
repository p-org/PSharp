using System;
using System.Collections.Generic;

using Microsoft.PSharp;
using Microsoft.PSharp.DynamicAnalysis;
using Microsoft.PSharp.Tooling;

namespace PingPong
{
    public class Test
    {
        static void Main(string[] args)
        {
            var configuration = DynamicAnalysisConfiguration.Create().
                WithVerbosityEnabled(2);
            SCTEngine.Create(configuration, Execute).Run();

            //Test.Execute();
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(ServerMachine));
        }
    }
}
