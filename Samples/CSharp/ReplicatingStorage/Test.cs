using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.SystematicTesting;

namespace ReplicatingStorage
{
    public class Test
    {
        public static void Main(string[] args)
        {
            /*var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);
            Console.ReadLine();
            */
            var configuration = Configuration.Create();
            configuration.CheckDataRaces = true;
            configuration.SuppressTrace = true;
            //configuration.Verbose = 2;
            configuration.SchedulingIterations = 1;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = false;

            var engine = TestingEngine.Create(configuration, Test.Execute).Run();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Environment));
        }
    }
}
