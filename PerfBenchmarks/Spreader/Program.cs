using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spreader
{
    /// <summary>
    /// Tests P# performance when creating a lot of machines
    /// Here, every machine creates 2 child machines and so on
    /// Creates 2^x - 1 machines, where x is the count passed into Spreader.Config
    /// This benchmark is adapted from https://github.com/ponylang/ponyc/tree/master/examples/spreader
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = Configuration.Create().WithVerbosityEnabled(0);
            var runtime = PSharpRuntime.Create(configuration);
            Benchmark(runtime).Wait();
            Console.ReadKey();
        }

        private static async Task Benchmark(PSharpRuntime runtime)
        {
            TaskCompletionSource<bool> hasCompleted = new TaskCompletionSource<bool>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            runtime.CreateMachine(typeof(Spreader), new Spreader.Config(null, 22, hasCompleted));
            await hasCompleted.Task;
            sw.Stop();
            Console.WriteLine("Took {0} ms", sw.ElapsedMilliseconds);
        }
    }
}
