using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnBenchmark
{
    /// <summary>
    /// Tests the P# runtime by creating a lot of actors, and also
    /// sending messages between them
    /// Adapted from the Akka.Net SpawnActor benchmark here:
    /// https://github.com/akkadotnet/akka.net/tree/dev/src/benchmark/SpawnBenchmark
    /// </summary>
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
