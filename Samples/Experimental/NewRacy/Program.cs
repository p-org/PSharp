using Microsoft.PSharp;
using Microsoft.PSharp.SystematicTesting;
using Microsoft.PSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewRacy
{
    class Program
    {
        public static void Main(string[] args)
        {
            var configuration = Configuration.Create();
            configuration.CheckDataRaces = true;
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 1;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = false;

            var engine = TestingEngine.Create(configuration, Program.Execute).Run();
        }

        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(GodMachine));
        }
    }
}
