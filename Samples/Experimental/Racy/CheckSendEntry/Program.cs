using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.SystematicTesting;

namespace CheckSendEntry
{
    class Program
    {
        public static void Main(string[] args)
        {
            /*PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(SendMachine));
            Console.WriteLine("Enter to exit");
            Console.ReadLine();
            */

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
            runtime.CreateMachine(typeof(SendMachine));
        }
    }
}
