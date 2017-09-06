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
                numberOfMessagesToPass = uint.MaxValue/512;
            }

            var configuration = Configuration.Create().WithVerbosityEnabled(0);                   
            var runtime = PSharpRuntime.Create(configuration);

            var resTask = RunRing(numberOfNodesInRing, numberOfRings, numberOfMessagesToPass, runtime);
            Console.WriteLine("Throughput with {0} rings of size {1} processing {2} messages each: {3} msgs/sec", 
                numberOfRings, numberOfNodesInRing, numberOfMessagesToPass, resTask.Result);
            Console.ReadKey();
        }

        private static async Task<long> RunRing(uint numberOfNodesInRing, uint numberOfRings, uint numberOfMessagesToPass, PSharpRuntime runtime)
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
            long throughput = elapsedMilliseconds == 0 ? -1 : totalMessagesReceived / elapsedMilliseconds * 1000;
            return throughput;
        }
    }
}
