// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class ReceiveStressTest : BaseTest
    {
        public ReceiveStressTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupTcsEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public int NumMessages;

            public SetupTcsEvent(TaskCompletionSource<bool> tcs, int numMessages)
            {
                this.Tcs = tcs;
                this.NumMessages = numMessages;
            }
        }

        internal class SetupIdEvent : Event
        {
            public MachineId Id;

            public int NumMessages;

            public SetupIdEvent(MachineId id, int numMessages)
            {
                this.Id = id;
                this.NumMessages = numMessages;
            }
        }

        private class Message : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var mid = this.CreateMachine(typeof(M2), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message());
                }
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.Receive(typeof(Message));
                }

                tcs.SetResult(true);
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var mid = this.CreateMachine(typeof(M4), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message());
                }
            }
        }

        private class M4 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            private int NumMessages;

            private int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                this.NumMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;
                this.Counter = 0;
            }

            private async Task HandleMessage()
            {
                await this.Receive(typeof(Message));
                this.Counter += 2;

                if (this.Counter == this.NumMessages)
                {
                    this.Tcs.SetResult(true);
                }
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var mid = this.CreateMachine(typeof(M6), new SetupIdEvent(this.Id, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message());
                    this.Receive(typeof(Message));
                }

                tcs.SetResult(true);
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var mid = (this.ReceivedEvent as SetupIdEvent).Id;
                var numMessages = (this.ReceivedEvent as SetupIdEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.Receive(typeof(Message));
                    this.Send(mid, new Message());
                }
            }
        }

        [Fact(Timeout = 15000)]
        public void StressTestReceiveEvent()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 10; i++)
            {
                var test = new Action<IMachineRuntime>((r) =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M1), new SetupTcsEvent(tcs, 10000));
                    Assert.True(tcs.Task.Result);
                });

                this.Run(configuration, test);
            }
        }

        [Fact(Timeout = 15000)]
        public void StressTestReceiveEventAlternate()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 10; i++)
            {
                var test = new Action<IMachineRuntime>((r) =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M3), new SetupTcsEvent(tcs, 10000));
                    Assert.True(tcs.Task.Result);
                });

                this.Run(configuration, test);
            }
        }

        [Fact(Timeout = 15000)]
        public void StressTestReceiveEventExchange()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 10; i++)
            {
                var test = new Action<IMachineRuntime>((r) =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M5), new SetupTcsEvent(tcs, 10000));
                    Assert.True(tcs.Task.Result);
                });

                this.Run(configuration, test);
            }
        }
    }
}
