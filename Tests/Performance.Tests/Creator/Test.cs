using System;

using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace Creator
{
    public class Test
    {
        static readonly int NumOfNodes = 100000;

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

            new TPLTest().Start(Test.NumOfNodes);

            profiler.StopMeasuringExecutionTime();
            Console.WriteLine("... TPL executed for '" +
                profiler.Results() + "' seconds.");
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Environment),
                new Environment.Config(Test.NumOfNodes));
        }
    }
}
