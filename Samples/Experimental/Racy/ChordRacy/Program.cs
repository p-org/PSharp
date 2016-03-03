using Microsoft.PSharp;
using Microsoft.PSharp.SystematicTesting;
using Microsoft.PSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChordRacy
{
    class Program
    {
        static void Main(string[] args)
        {
            /*PSharpRuntime runtime = PSharpRuntime.Create();
            MachineId cluster = runtime.CreateMachine(typeof(Cluster));
            runtime.SendEvent(cluster, new Cluster.eInitialize(new Tuple<int, List<int>, List<int>>(
                3,
                new List<int> { 0, 1, 3 },
                new List<int> { 1, 2, 6 })));
            Console.WriteLine("Done");
            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();*/

            var configuration = Configuration.Create();
            configuration.CheckDataRaces = true;
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 1;
            configuration.SchedulingStrategy = SchedulingStrategy.Random;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngine.Create(configuration, Program.Execute).Run();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            MachineId cluster = runtime.CreateMachine(typeof(Cluster));
            /*runtime.SendEvent(cluster, new Cluster.eInitialize(new Tuple<int, List<int>, List<int>>(
                3,
                new List<int> { 0, 1, 3 },
                new List<int> { 1, 2, 6 })));*/
            Console.WriteLine("Done");
        }
    }
}
