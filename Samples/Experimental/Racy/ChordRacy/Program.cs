using Microsoft.PSharp;
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
            PSharpRuntime runtime = PSharpRuntime.Create();
            MachineId cluster = runtime.CreateMachine(typeof(Cluster));
            runtime.SendEvent(cluster, new Cluster.eInitialize(new Tuple<int, List<int>, List<int>>(
                3,
                new List<int> { 0, 1, 3 },
                new List<int> { 1, 2, 6 })));
            Console.WriteLine("Done");
            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();
        }
    }
}
