using System;
using System.Collections.Generic;

using Microsoft.PSharp;
using Microsoft.PSharp.DynamicAnalysis;
using Microsoft.PSharp.Utilities;

namespace NodeManager
{
    public class Test
    {
        static void Main(string[] args)
        {
            var configuration = Configuration.Create().
                WithLivenessCheckingEnabled().
                WithNumberOfIterations(10).
                WithVerbosityEnabled(2);
            SCTEngine.Create(configuration, Execute).Run();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(Environment));
        }
    }
}
