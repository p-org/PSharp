// ------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Running;
using Microsoft.PSharp.Benchmarking.Creation;
using Microsoft.PSharp.Benchmarking.Messaging;

namespace Microsoft.PSharp.Benchmarking
{
    /// <summary>
    /// The P# performance benchmark runner.
    /// </summary>
    internal class Program
    {
#pragma warning disable CA1801 // Parameter not used
        private static void Main(string[] args)
        {
            // BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
            BenchmarkRunner.Run<MachineCreationThroughputBenchmark>();
            BenchmarkRunner.Run<ExchangeEventLatencyBenchmark>();
            BenchmarkRunner.Run<SendEventThroughputBenchmark>();
            BenchmarkRunner.Run<DequeueEventThroughputBenchmark>();
        }
#pragma warning restore CA1801 // Parameter not used
    }
}
