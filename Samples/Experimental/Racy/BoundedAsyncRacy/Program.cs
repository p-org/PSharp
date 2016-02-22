using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.SystematicTesting;

namespace BoundedAsyncRacy
{
    /// <summary>
    /// This is an example of using P#.
    /// 
    /// This example implements an asynchronous scheduler communicating
    /// with a number of processes under a predefined bound.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            /*PSharpRuntime runtime = PSharpRuntime.Create();
            Program.Execute(runtime);
            Console.WriteLine("Done");
            Console.ReadLine();*/

            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngine.Create(configuration, Program.Execute).Run();
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Scheduler));
        }
    }
}
