using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = Configuration.Create().WithVerbosityEnabled(0);

            var runtime = PSharpRuntime.Create(configuration);
            var rootMachine = runtime.CreateMachine(typeof(RootMachine));
            runtime.SendEvent(rootMachine, new RootMachine.Run(5));
            Console.ReadLine();
        }
    }
}
