using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.SystematicTesting;

namespace ChainReplicationRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Chain Replication protocol
    /// from OSDI'04.
    /// </summary>
    /*public class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(GodMachine));
            Console.WriteLine("Done");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }*/

    public class Program
    {
        public static void Main(string[] args)
        {
            /*PSharpRuntime runtime = PSharpRuntime.Create();
            Program.Execute(runtime);
            Console.WriteLine("Done");
            Console.ReadLine();*/

            var configuration = Configuration.Create();
            configuration.CheckDataRaces = true;
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 1;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = false;

            var engine = TestingEngine.Create(configuration, Program.Execute).Run();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(GodMachine));
        }
    }
}
