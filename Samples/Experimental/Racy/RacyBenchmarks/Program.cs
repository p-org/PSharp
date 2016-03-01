using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.SystematicTesting;
//using Microsoft.PSharp.SystematicTesting;

namespace BasicPaxosRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements Lamport's Paxos distributed
    /// concencus algorithm.
    /// </summary>
    public class Program
    {
        public static void Execute(PSharpRuntime runtime)
        {
            /*
            PSharpRuntime runtime = PSharpRuntime.Create();
            */

            runtime.CreateMachine(typeof(GodMachine));
        }
        public static void Go()
        {
            
            PSharpRuntime runtime = PSharpRuntime.Create();
            
            runtime.CreateMachine(typeof(GodMachine));
        }
        public static void Main(string[] args)
        {
            /*Console.WriteLine("Starting BasicPaxos");
            Go();
            Console.WriteLine("Done Execution");
            Console.WriteLine("[Press any key to exit]");
            Console.ReadLine();*/
            

            var configuration = Configuration.Create();
            configuration.CheckDataRaces = true;
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 1000;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngine.Create(configuration, Program.Execute).Run();
        }
    }
}
