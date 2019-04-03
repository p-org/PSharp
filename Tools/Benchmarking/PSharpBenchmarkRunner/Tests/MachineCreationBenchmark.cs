﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.Benchmarking
{
    [ClrJob(baseline: true), CoreJob]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class MachineCreationBenchmark
    {
        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public int NumMachines;
            public int Counter;
            public bool DoHalt;

            public SetupEvent(TaskCompletionSource<bool> tcs, int numMachines, bool doHalt)
            {
                this.Tcs = tcs;
                this.NumMachines = numMachines;
                this.Counter = 0;
                this.DoHalt = doHalt;
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var numMachines = (this.ReceivedEvent as SetupEvent).NumMachines;
                var doHalt = (this.ReceivedEvent as SetupEvent).DoHalt;

                var counter = Interlocked.Increment(ref (this.ReceivedEvent as SetupEvent).Counter);
                if (counter == numMachines)
                {
                    tcs.TrySetResult(true);
                }

                if (doHalt)
                {
                    this.Raise(new Halt());
                }
            }
        }

        [Params(10000, 100000)]
        public int NumMachines { get; set; }

        [Params(true, false)]
        public bool DoHalt { get; set; }

        [Benchmark]
        public void MeasureMachineCreation()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);

            var tcs = new TaskCompletionSource<bool>();
            var e = new SetupEvent(tcs, this.NumMachines, this.DoHalt);

            for (int idx = 0; idx < this.NumMachines; idx++)
            {
                runtime.CreateMachine(typeof(M), null, e, null);
            }

            tcs.Task.Wait();
        }
    }
}
