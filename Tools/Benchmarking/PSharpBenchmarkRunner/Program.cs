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
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
            BenchmarkRunner.Run<MachineCreationBenchmark>();
            BenchmarkRunner.Run<MessagingLatencyBenchmark>();
            BenchmarkRunner.Run<MessagingThroughputBenchmark>();
        }
    }
}
