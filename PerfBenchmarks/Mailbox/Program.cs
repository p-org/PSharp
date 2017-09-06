using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mailbox
{
    class Program
    {
        static void Main(string[] args)
        {
            uint numberOfSendingMachines;
            if (args.Length == 0 || !uint.TryParse(args[0], out numberOfSendingMachines))
            {
                numberOfSendingMachines = 100u;
            }

            uint numberOfMessages;
            if (args.Length == 0 || !uint.TryParse(args[1], out numberOfMessages))
            {
                numberOfMessages = 5000u;
            }

            var configuration = Configuration.Create().WithVerbosityEnabled(0);
            var runtime = PSharpRuntime.Create(configuration);
            var resTask = RunBenchmark(numberOfSendingMachines, numberOfMessages, runtime);
            Console.WriteLine("Throughput with {0} mailers sending {1} messages each: {2} msgs/sec",
                numberOfSendingMachines, numberOfMessages, resTask.Result);
            //Console.ReadKey();
        }

        private static async Task<long> RunBenchmark(uint numberOfSendingMachines, uint numberOfMessages, PSharpRuntime runtime)
        {
            var sw = Stopwatch.StartNew();
            long totalMessageCount = (long)numberOfSendingMachines * (long)numberOfMessages;
            var hasCompleted = new TaskCompletionSource<bool>();
            var server = runtime.CreateMachine(typeof(ServerMachine), new ServerMachine.Config(totalMessageCount, hasCompleted));


            Parallel.For(0, numberOfSendingMachines, index =>
            {
                runtime.CreateMachine(typeof(MailerMachine), new MailerMachine.Config(server, numberOfMessages));
            });
            await hasCompleted.Task;
            sw.Stop();
            
            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            long throughput = elapsedMilliseconds == 0 ? -1 : totalMessageCount / elapsedMilliseconds * 1000;
            return throughput;
        }
    }
}
