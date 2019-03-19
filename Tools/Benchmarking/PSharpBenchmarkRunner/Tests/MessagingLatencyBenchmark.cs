// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.Benchmarking
{
    [ClrJob(baseline: true), CoreJob]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class MessagingLatencyBenchmark
    {
        private class SetupMasterEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public long NumMessages;

            public SetupMasterEvent(TaskCompletionSource<bool> tcs, long numMessages)
            {
                this.Tcs = tcs;
                this.NumMessages = numMessages;
            }
        }

        private class SetupWorkerEvent : Event
        {
            public MachineId Master;

            internal SetupWorkerEvent(MachineId master)
            {
                this.Master = master;
            }
        }

        private class Message : Event { }

        private class Master : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private MachineId Worker;
            private long NumMessages;
            private long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(SendMessage))]
            private class Init : MachineState { }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupMasterEvent).Tcs;
                this.NumMessages = (this.ReceivedEvent as SetupMasterEvent).NumMessages;
                this.Worker = this.CreateMachine(typeof(Worker), new SetupWorkerEvent(this.Id));
                this.SendMessage();
            }

            private void SendMessage()
            {
                if (this.Counter == this.NumMessages)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Counter++;
                    this.Send(this.Worker, new Message());
                }
            }
        }

        private class Worker : Machine
        {
            private MachineId Master;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(SendMessage))]
            private class Init : MachineState { }

            private void InitOnEntry()
            {
                this.Master = (this.ReceivedEvent as SetupWorkerEvent).Master;
            }

            private void SendMessage()
            {
                this.Send(this.Master, new Message());
            }
        }

        [Params(10000, 100000)]
        public int NumMessages { get; set; }

        [Benchmark]
        public void MeasureMessagingLatency()
        {
            var tcs = new TaskCompletionSource<bool>();

            var runtime = new ProductionRuntime();
            runtime.CreateMachine(typeof(Master), null,
                new SetupMasterEvent(tcs, this.NumMessages),
                null);

            tcs.Task.Wait();
        }
    }
}
