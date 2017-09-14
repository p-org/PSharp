using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring
{
    /// <summary>
    /// Creates a set of rings of Machines
    /// Each ring then passes around N messages.
    /// This benchmark is adapted from https://github.com/ponylang/ponyc/tree/master/examples/ring
    /// </summary>
    public class Program
    {
        /*
         * args:
         * -- numberOfNodesInRing: number of nodes in each ring
         * -- numberOfRings: number of rings
         * -- numberOfMessagesToPass: number of messages to pass around each ring
         */
        public static void Main(params string[] args)
        {
            uint numberOfNodesInRing;
            if (args.Length == 0 || !uint.TryParse(args[0], out numberOfNodesInRing))
            {
                numberOfNodesInRing = 5u;
            }

            uint numberOfRings;
            if (args.Length == 0 || !uint.TryParse(args[1], out numberOfRings))
            {
                numberOfRings = 3u;
            }

            uint numberOfMessagesToPass;
            if (args.Length == 0 || !uint.TryParse(args[2], out numberOfMessagesToPass))
            {
                numberOfMessagesToPass = uint.MaxValue / 256;
            }

            var configuration = Configuration.Create().WithVerbosityEnabled(0);
            var runtime = PSharpRuntime.Create(configuration);

            List<double> throughputResults = new List<double>();
            List<double> timeResults = new List<double>();
            foreach (var msgCount in new uint[4] { 1000, 10000, 100000, 1000000 })
            {
                Console.WriteLine("Sending {0} messages", msgCount);
                for (int i = 0; i < 5; i++)
                {
                    var resTask = RunRing(numberOfNodesInRing, numberOfRings, msgCount, runtime);
                    Console.WriteLine("Throughput with {0} rings of size {1} processing {2} messages each: {3} msgs/sec. Total time {4} sec",
                        numberOfRings, numberOfNodesInRing, msgCount, resTask.Result.Item2, resTask.Result.Item1);
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

        private static async Task<Tuple<double, double>> RunRing(uint numberOfNodesInRing, uint numberOfRings, uint numberOfMessagesToPass, PSharpRuntime runtime)
        {
            var totalWatch = Stopwatch.StartNew();
            List<MachineId> leaders = new List<MachineId>();
            List<Task> initSignals = new List<Task>();
            var completionSource = new TaskCompletionSource<bool>();

            MachineId supervisor = runtime.CreateMachine(typeof(SupervisorMachine), 
                new SupervisorMachine.Config(numberOfRings, completionSource));

            for (int i = 0; i < numberOfRings; i++)
            {
                // leader for the current ring
                MachineId ringLeader = runtime.CreateMachine(typeof(RingNode));
                MachineId prev = ringLeader, current = ringLeader;
                for (int j = 1; j < numberOfNodesInRing; j++)
                {
                    TaskCompletionSource<bool> iSource = new TaskCompletionSource<bool>();
                    initSignals.Add(iSource.Task);
                    current = runtime.CreateMachine(typeof(RingNode));
                    runtime.SendEvent(prev, new RingNode.Config(current, iSource, supervisor));
                    prev = current;
                }

                var initSource = new TaskCompletionSource<bool>();
                initSignals.Add(initSource.Task);
                runtime.SendEvent(current, new RingNode.Config(ringLeader, initSource, supervisor));
                leaders.Add(ringLeader);
            }
     
            await Task.WhenAll(initSignals);
            var setupTime = totalWatch.Elapsed;
            Console.WriteLine("Initialization {0} msec", setupTime.Milliseconds);
            var sw = Stopwatch.StartNew();           
            leaders.ForEach(x => runtime.SendEvent(x, new RingNode.Pass(numberOfMessagesToPass)));
            await completionSource.Task;
            sw.Stop();
            totalWatch.Stop();

            long totalMessagesReceived = ((long)numberOfMessagesToPass) * ((long)numberOfRings);
            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            Console.WriteLine("Computing throughtput as {0}/{1} * 1000", totalMessagesReceived, elapsedMilliseconds);
            double throughput = elapsedMilliseconds == 0 ? -1 : (double)totalMessagesReceived / (double)elapsedMilliseconds * 1000;
            return new Tuple<double, double>(elapsedMilliseconds/1000.0, throughput);
        }
    }
}
