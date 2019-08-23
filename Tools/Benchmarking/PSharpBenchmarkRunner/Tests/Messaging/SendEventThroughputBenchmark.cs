﻿// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.Benchmarking.Messaging
{
    [ClrJob(baseline: true), CoreJob]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class SendEventThroughputBenchmark
    {
        private class SetupProducerEvent : Event
        {
            public TaskCompletionSource<bool> TcsSetup;
            public TaskCompletionSource<bool> TcsExperiment;
            public long NumConsumers;
            public long NumMessages;

            public SetupProducerEvent(TaskCompletionSource<bool> tcsSetup, TaskCompletionSource<bool> tcsExperiment,
                long numConsumers, long numMessages)
            {
                this.TcsSetup = tcsSetup;
                this.TcsExperiment = tcsExperiment;
                this.NumConsumers = numConsumers;
                this.NumMessages = numMessages;
            }
        }

        private class SetupConsumerEvent : Event
        {
            public MachineId Producer;
            public long NumMessages;

            internal SetupConsumerEvent(MachineId producer, long numMessages)
            {
                this.Producer = producer;
                this.NumMessages = numMessages;
            }
        }

        private class StartExperiment : Event
        {
        }

        private class Message : Event
        {
        }

        private class Ack : Event
        {
        }

        private class Producer : Machine
        {
            private TaskCompletionSource<bool> TcsSetup;
            private TaskCompletionSource<bool> TcsExperiment;
            private MachineId[] Consumers;
            private long NumConsumers;
            private long NumMessages;
            private long Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Ack), nameof(HandleCreationAck))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.TcsSetup = (this.ReceivedEvent as SetupProducerEvent).TcsSetup;
                this.TcsExperiment = (this.ReceivedEvent as SetupProducerEvent).TcsExperiment;
                this.NumConsumers = (this.ReceivedEvent as SetupProducerEvent).NumConsumers;
                this.NumMessages = (this.ReceivedEvent as SetupProducerEvent).NumMessages;

                this.Consumers = new MachineId[this.NumConsumers];
                this.Counter = 0;

                for (int i = 0; i < this.NumConsumers; i++)
                {
                    this.Consumers[i] = this.CreateMachine(
                        typeof(Consumer),
                        new SetupConsumerEvent(this.Id, this.NumMessages / this.NumConsumers));
                }
            }

            private void HandleCreationAck()
            {
                this.Counter++;
                if (this.Counter == this.NumConsumers)
                {
                    this.TcsSetup.SetResult(true);
                    this.Goto<Experiment>();
                }
            }

            [OnEventDoAction(typeof(StartExperiment), nameof(Run))]
            [OnEventDoAction(typeof(Ack), nameof(HandleMessageAck))]
            private class Experiment : MachineState
            {
            }

            private void Run()
            {
                this.Counter = 0;
                for (int i = 0; i < this.NumMessages; i++)
                {
                    this.Send(this.Consumers[i % this.NumConsumers], new Message());
                }
            }

            private void HandleMessageAck()
            {
                this.Counter++;
                if (this.Counter == this.NumConsumers)
                {
                    this.TcsExperiment.SetResult(true);
                }
            }
        }

        private class Consumer : Machine
        {
            private MachineId Producer;
            private long NumMessages;
            private long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Producer = (this.ReceivedEvent as SetupConsumerEvent).Producer;
                this.NumMessages = (this.ReceivedEvent as SetupConsumerEvent).NumMessages;
                this.Send(this.Producer, new Ack());
            }

            private void HandleMessage()
            {
                this.Counter++;
                if (this.Counter == this.NumMessages)
                {
                    this.Send(this.Producer, new Ack());
                }
            }
        }

        [Params(10, 100, 1000, 10000)]
        public int NumConsumers { get; set; }

        private static int NumMessages => 1000000;

        private ProductionRuntime Runtime;
        private MachineId ProducerMachine;
        private TaskCompletionSource<bool> ExperimentAwaiter;

        [IterationSetup]
        public void IterationSetup()
        {
            var configuration = Configuration.Create();
            this.Runtime = new ProductionRuntime(configuration);
            this.ExperimentAwaiter = new TaskCompletionSource<bool>();

            var tcs = new TaskCompletionSource<bool>();
            this.ProducerMachine = this.Runtime.CreateMachine(typeof(Producer), null,
                new SetupProducerEvent(tcs, this.ExperimentAwaiter, this.NumConsumers, NumMessages));

            tcs.Task.Wait();
        }

        [Benchmark]
        public void MeasureEventSendingThroughput()
        {
            this.Runtime.SendEvent(this.ProducerMachine, new StartExperiment());
            this.ExperimentAwaiter.Task.Wait();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            this.Runtime = null;
            this.ProducerMachine = null;
            this.ExperimentAwaiter = null;
        }
    }
}
