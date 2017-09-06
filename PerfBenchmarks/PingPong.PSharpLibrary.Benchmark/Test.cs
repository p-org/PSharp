using System;
using Microsoft.PSharp;
using System.Runtime;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace PingPong.PSharpLibrary
{
    /// <summary>
    /// A simple PingPong application written using P# as a C# library.
    /// 
    /// The P# runtime starts by creating the P# machine 'NetworkEnvironment'. The
    /// 'NetworkEnvironment' machine then creates a 'Server' and a 'Client' machine,
    /// which then communicate by sending 'Ping' and 'Pong' events to each other for
    /// a limited amount of turns.
    /// 
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of P#.
    /// </summary>
    public class Program
    {
        public static uint CpuSpeed()
        {
#if THREADS
            var mo = new System.Management.ManagementObject("Win32_Processor.DeviceID='CPU0'");
            var sp = (uint)(mo["CurrentClockSpeed"]);
            mo.Dispose();
            return sp;
#else
            return 0;
#endif
        }

        private static long GetTotalMessagesReceived(long numberOfRepeats)
        {
            return numberOfRepeats * 2;
        }

        
        private static void Main(params string[] args)
        {
            uint timesToRun;
            if (args.Length == 0 || !uint.TryParse(args[0], out timesToRun))
            {
                timesToRun = 1u;
            }
           
            var configuration = Configuration.Create().WithVerbosityEnabled(0);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);
           
            Start(timesToRun, runtime);
            Console.ReadKey();
        }

        public static IEnumerable<int> GetThroughputSettings()
        {
            yield return 1;
            yield return 5;
            yield return 10;
            yield return 15;
            for (int i = 20; i < 100; i += 10)
            {
                yield return i;
            }
            for (int i = 100; i < 1000; i += 100)
            {
                yield return i;
            }
        }
      
        private static async void Start(uint timesToRun, PSharpRuntime runtime)
        {
            const int repeatFactor = 500;
            const long repeat = 30000L * repeatFactor;

            var processorCount = Environment.ProcessorCount;
            if (processorCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to read processor count..");
                return;
            }

#if THREADS
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

            Console.WriteLine("Worker threads:         {0}", workerThreads);
            Console.WriteLine("OSVersion:              {0}", Environment.OSVersion);
#endif
            Console.WriteLine("ProcessorCount:         {0}", processorCount);
            Console.WriteLine("ClockSpeed:             {0} MHZ", CpuSpeed());
            Console.WriteLine("Machine Count:            {0}", processorCount * 2);
            Console.WriteLine("Messages sent/received: {0}  ({0:0e0})", GetTotalMessagesReceived(repeat));
            Console.WriteLine("Is Server GC:           {0}", GCSettings.IsServerGC);
            Console.WriteLine();

            // Warm up
            Console.WriteLine("Warm up");
            await Benchmark(1, 1, 1, PrintStats.StartTimeOnly, -1, -1, runtime);
            Console.WriteLine(" ms");
            Console.WriteLine("Warm Up Complete");
            Console.Write("Throughput, Msgs/sec, Start [ms], Total [ms]");
            Console.WriteLine();

            for (var i = 0; i < timesToRun; i++)
            {
                var redCountActorBase = 0;                
                var bestThroughputActorBase = 0L;                
                foreach (var throughput in GetThroughputSettings())
                {
                    // Console.WriteLine("Repeat {0}", repeat);
                    var result1 = await Benchmark(throughput, processorCount, repeat, PrintStats.LineStart | PrintStats.Stats, bestThroughputActorBase, redCountActorBase, runtime);
                    bestThroughputActorBase = result1.Item2;
                    redCountActorBase = result1.Item3;                    
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Done..");
        }

        private static async Task<Tuple<bool, long, int>> Benchmark(int factor, int numberOfClients, long numberOfRepeats, PrintStats printStats, long bestThroughput, int redCount, PSharpRuntime runtime)
        {
            var totalMessagesReceived = GetTotalMessagesReceived(numberOfRepeats);
            //times 2 since the client and the server both send messages
            long repeatsPerClient = numberOfRepeats / numberOfClients;
            var totalWatch = Stopwatch.StartNew();            
            var clients = new List<MachineId>();
            var completionSignals = new List<Task>();
            var initializationSignals = new List<Task>();

            for (int i = 0; i < numberOfClients; i++)
            {
                var hasCompleted = new TaskCompletionSource<bool>();
                var hasInitialized = new TaskCompletionSource<bool>();
                completionSignals.Add(hasCompleted.Task);
                initializationSignals.Add(hasInitialized.Task);
                var server = runtime.CreateMachine(typeof(Server));
                var client = runtime.CreateMachine(typeof(Client), new Client.Config(server, hasCompleted, hasInitialized, repeatsPerClient));                
                clients.Add(client);                
            }
            await Task.WhenAll(initializationSignals);
            var setupTime = totalWatch.Elapsed;
            var sw = Stopwatch.StartNew();
            var run = new Messages.Run();
            clients.ForEach(c => runtime.SendEvent(c, run));
            await Task.WhenAll(completionSignals.ToArray());
            sw.Stop();            
            totalWatch.Stop();

            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            long throughput = elapsedMilliseconds == 0 ? -1 : totalMessagesReceived / elapsedMilliseconds * 1000;
            var foregroundColor = Console.ForegroundColor;
            if (throughput >= bestThroughput)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                bestThroughput = throughput;
                redCount = 0;
            }
            else
            {
                redCount++;
                Console.ForegroundColor = ConsoleColor.Red;
            }
            if (printStats.HasFlag(PrintStats.StartTimeOnly))
            {
                Console.Write("{0,5}", setupTime.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture));
            }
            else
            {
                if (printStats.HasFlag(PrintStats.LineStart))
                    Console.Write("{0,10}, ", factor);
                if (printStats.HasFlag(PrintStats.Stats))
                    Console.Write("{0,8}, {1,10}, {2,10}", throughput, setupTime.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture), totalWatch.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture));
            }
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine();
            return Tuple.Create(redCount <= 3, bestThroughput, redCount);
        }

        [Flags]
        public enum PrintStats
        {
            No = 0,
            LineStart = 1,
            Stats = 2,
            StartTimeOnly = 32768,
        }

    }
}
