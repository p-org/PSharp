// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Running;

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
            BenchmarkRunner.Run<MachineCreationBenchmark>();
            BenchmarkRunner.Run<MessagingLatencyBenchmark>();
            BenchmarkRunner.Run<MessagingThroughputBenchmark>();
        }
#pragma warning restore CA1801 // Parameter not used
    }
}
