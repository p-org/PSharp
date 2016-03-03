using Microsoft.PSharp;
using Microsoft.PSharp.SystematicTesting;
using Microsoft.PSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GermanRacy
{
    /*class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            MachineId id = runtime.CreateMachine(typeof(Host));
            runtime.SendEvent(id, new Host.eInitialize(3));
            Console.WriteLine("Done; Press any key to exit");
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
            configuration.SchedulingIterations = 11;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngine.Create(configuration, Program.Execute).Run();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            MachineId hst = runtime.CreateMachine(typeof(Host));
            //runtime.SendEvent(hst, new Host.eInitialize(3));
        }
    }
}
