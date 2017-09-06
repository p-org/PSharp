using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spreader
{
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
