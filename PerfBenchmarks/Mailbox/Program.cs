using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mailbox
{
    /// <summary>
    /// Tests P# performance when sending a lot of messages to a machine
    /// This benchmark is adapted from https://github.com/ponylang/ponyc/tree/master/examples/mailbox
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            uint numberOfSendingMachines;
            if (args.Length == 0 || !uint.TryParse(args[0], out numberOfSendingMachines))
            {
                numberOfSendingMachines = 10u;
            }

            uint numberOfMessages;
            if (args.Length == 0 || !uint.TryParse(args[1], out numberOfMessages))
            {
                numberOfMessages = 50000u;
            }

            List<double> throughputResults = new List<double>();
            List<double> timeResults = new List<double>();

            foreach (var msgCount in new uint[4] { 1000, 10000, 100000, 1000000 })
            {
                Console.WriteLine("Sending {0} messages", msgCount);
                for (int i = 0; i < 5; i++)
                {
                    var configuration = Configuration.Create().WithVerbosityEnabled(0);
                    var runtime = PSharpRuntime.Create(configuration);
                    var resTask = RunBenchmark(numberOfSendingMachines, msgCount, runtime);
                    Console.WriteLine("Throughput with {0} mailers sending {1} messages each: {2} msgs/sec in {3} sec",
                        numberOfSendingMachines, msgCount, resTask.Result.Item2, resTask.Result.Item1);
                    throughputResults.Add(resTask.Result.Item2);
                    timeResults.Add(resTask.Result.Item1);
                }
                Console.WriteLine("Avg. throughput {0}", throughputResults.Average(r => r));
                Console.WriteLine("Avg. time {0}", timeResults.Average(r => r));
                throughputResults.Clear();
                timeResults.Clear();
            }
            Console.ReadKey();
        }

        private static async Task<Tuple<double, double>> RunBenchmark(uint numberOfSendingMachines, uint numberOfMessages, PSharpRuntime runtime)
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
            // Console.WriteLine("Computing throughput as {0}/{1} * 1000", totalMessageCount, elapsedMilliseconds);
            double throughput = elapsedMilliseconds == 0 ? -1 : (double)totalMessageCount / (double)elapsedMilliseconds * 1000;
            return new Tuple<double, double>(elapsedMilliseconds/1000.0, throughput);
        }
    }
}
