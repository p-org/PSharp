using System;

using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace NBody
{
    public class Test
    {
        static readonly int NumOfBodies = 1000;
        static readonly int NumOfSteps = 100;

        static void Main(string[] args)
        {
            Profiler profiler = new Profiler();
            profiler.StartMeasuringExecutionTime();

            var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);
            runtime.Wait();

            profiler.StopMeasuringExecutionTime();
            Console.WriteLine("... P# executed for '" +
                profiler.Results() + "' seconds.");

            profiler.StartMeasuringExecutionTime();

            new TPLTest().Start(Test.NumOfBodies, Test.NumOfSteps);

            profiler.StopMeasuringExecutionTime();
            Console.WriteLine("... TPL executed for '" +
                profiler.Results() + "' seconds.");
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Simulation),
                new Simulation.Config(Test.NumOfBodies, Test.NumOfSteps));
        }
    }
}
