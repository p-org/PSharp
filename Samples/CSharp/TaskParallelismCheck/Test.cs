using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace TaskParallelismCheck
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
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = Microsoft.PSharp.SystematicTesting.TestingEngine.Create(configuration, Test.Execute).Run();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(TaskCreator));
        }
    }
}
