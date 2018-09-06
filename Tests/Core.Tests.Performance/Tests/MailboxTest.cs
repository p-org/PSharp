// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    [Config(typeof(Configuration))]
    public class MailboxTest
    {
        class Client : Machine
        {
            internal class Configure : Event
            {
                internal MachineId Server;
                internal long NumberOfSends;

                internal Configure(MachineId server, long numberOfSends)
                {
                    this.Server = server;
                    this.NumberOfSends = numberOfSends;
                }
            }

            internal class Ping : Event { }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var server = (this.ReceivedEvent as Configure).Server;
                var numberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                for (int i = 0; i < numberOfSends; i++)
                {
                    this.Send(server, new Ping());
                }
            }
        }

        class Server : Machine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;
                internal long NumberOfClients;
                internal long NumberOfSends;

                internal Configure(TaskCompletionSource<bool> tcs, long numberOfClients, long numberOfSends)
                {
                    this.TCS = tcs;
                    this.NumberOfClients = numberOfClients;
                    this.NumberOfSends = numberOfSends;
                }
            }

            TaskCompletionSource<bool> TCS;
            long NumberOfClients;
            long NumberOfSends;
            long MaxValue = 0;
            long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Client.Ping), nameof(Pong))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.TCS = (this.ReceivedEvent as Configure).TCS;
                this.NumberOfClients = (this.ReceivedEvent as Configure).NumberOfClients;
                this.NumberOfSends = (this.ReceivedEvent as Configure).NumberOfSends;
                this.MaxValue = this.NumberOfClients * this.NumberOfSends;
                for (int i = 0; i < this.NumberOfClients; i++)
                {
                    this.CreateMachine(typeof(Client), new Client.Configure(this.Id, this.NumberOfSends));
                }
            }

            void Pong()
            {
                this.Counter++;
                if (this.Counter == this.MaxValue)
                {
                    this.TCS.SetResult(true);
                }
            }
        }

        [Params(1, 2, 4)]
        public int Clients { get; set; }

        [Params(100, 1000, 5000, 10000)]
        public int EventsPerClient { get; set; }

        [Benchmark]
        public void SendMessages()
        {
            var tcs = new TaskCompletionSource<bool>();

            var runtime = new StateMachineRuntime();
            runtime.CreateMachine(typeof(Server), null,
                new Server.Configure(tcs, this.Clients, this.EventsPerClient),
                null);

            tcs.Task.Wait();
        }

        //[Benchmark(Baseline = true)]
        //public void CreateTasks()
        //{
        //    Task[] tasks = new Task[Size];
        //    for (int idx = 0; idx < Size; idx++)
        //    {
        //        var task = new Task(() => { return; });
        //        task.Start();
        //        tasks[idx] = task;
        //    }

        //    Task.WaitAll(tasks);
        //}
    }
}
