using Microsoft.PSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StressLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = Configuration.Create().WithVerbosityEnabled(2);
            var runtime = PSharpRuntime.Create(configuration);
            Benchmark(runtime).Wait();
            Console.ReadKey();
        }

        private static async Task Benchmark(PSharpRuntime runtime)
        {
            TaskCompletionSource<bool> hasCompleted = new TaskCompletionSource<bool>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ConcurrentQueue<MachineId> machines = new ConcurrentQueue<MachineId>();
            Parallel.For(0, 1000, index =>
            {
                machines.Enqueue(runtime.CreateMachine(typeof(SimpleMachine), new SetContextMessage(runtime)));
            });

            await Task.Delay(TimeSpan.FromSeconds(10));

            foreach (var machine in machines)
            {
                runtime.SendEvent(machine, new Halt());
            }

            sw.Stop();

            Console.WriteLine("Took {0} ms", sw.ElapsedMilliseconds);
        }
    }
}
