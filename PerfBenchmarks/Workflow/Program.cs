using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Workflow.WorkFlowEvents;

namespace Workflow
{
    class Program
    {
        static void Main(string[] args)
        {
            uint numberOfSources;
            if (args.Length == 0 || !uint.TryParse(args[0], out numberOfSources))
            {
                numberOfSources = 2u;
            }

            uint numberOfMessages;
            if (args.Length == 0 || !uint.TryParse(args[1], out numberOfMessages))
            {
                numberOfMessages = 1000000u;
            }

            var configuration = Configuration.Create().WithVerbosityEnabled(0);
            var runtime = PSharpRuntime.Create(configuration);

            var resTask = RunWorkFlow(numberOfSources, numberOfMessages, runtime);
            Console.WriteLine("Throughput with {0} sources generating {1} messages each: {2} msgs/sec",
                numberOfSources, numberOfMessages, resTask.Result);
            Console.ReadKey();
        }

        private static async Task<long> RunWorkFlow(uint numberOfSources, uint numberOfMessages, PSharpRuntime runtime)
        {
            var totalWatch = Stopwatch.StartNew();
            List<MachineId> sources = new List<MachineId>();
            var intermediateInitializationSource = new TaskCompletionSource<bool>();
            var sinkInitializationSource = new TaskCompletionSource<bool>();
            var supervisorCompletionSource = new TaskCompletionSource<bool>();
            List<Task> initSignals = new List<Task>();
            initSignals.Add(intermediateInitializationSource.Task);
            initSignals.Add(sinkInitializationSource.Task);

            // We additionally use an intermediate stage, and a sink, so the total 
            // number of nodes in the workflow is numberOfSources + 2
            MachineId supervisor = runtime.CreateMachine(typeof(WorkFlowSupervisor),
                new WorkFlowSupervisor.Config(numberOfSources + 2, supervisorCompletionSource));

            MachineId sink = runtime.CreateMachine(typeof(WorkFlowSinkNode),
                new WorkFlowSinkNode.Config(sinkInitializationSource, 1, supervisor));

            List<MachineId> next = new List<MachineId>();
            next.Add(sink);
            long numberOfMessagesToIntermediate = (long)numberOfSources * (long)numberOfMessages;

            MachineId intermediate = runtime.CreateMachine(typeof(WorkFlowIntermediateNode),
                new WorkFlowIntermediateNode.Config(next, intermediateInitializationSource, numberOfMessagesToIntermediate, supervisor));

            var l = new List<MachineId>();
            l.Add(intermediate);
            for (int i = 0; i < numberOfSources; i++)
            {
                TaskCompletionSource<bool> iSource = new TaskCompletionSource<bool>();
                initSignals.Add(iSource.Task);
                var source = runtime.CreateMachine(typeof(WorkFlowSourceNode),
                    new WorkFlowSourceNode.Config(l, iSource, numberOfMessages, supervisor));
                sources.Add(source);
            }

            await Task.WhenAll(initSignals);
            var setupTime = totalWatch.Elapsed;
            Console.WriteLine("Initialization {0} msec", setupTime.Milliseconds);
            var sw = Stopwatch.StartNew();
            sources.ForEach(x => runtime.SendEvent(x, new WorkFlowStartEvent()));
            await supervisorCompletionSource.Task; // task denoting the supervisor's completion
            sw.Stop();
            totalWatch.Stop();

            long totalMessagesReceived = ((numberOfMessagesToIntermediate) * 2) + 1; // because the intermediate forwards them again
            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            long throughput = elapsedMilliseconds == 0 ? -1 : totalMessagesReceived / elapsedMilliseconds * 1000;
            return throughput;
        }
    }
}
