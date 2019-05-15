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
    public class ReceiveEventStressTestEllab : BaseTest
    {
        public ReceiveEventStressTestEllab(ITestOutputHelper output)
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

        internal class SetupEventBool : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public int NumMessages;

            public SetupEventBool(TaskCompletionSource<bool> tcs, int numMessages)
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
            public int Counter;

            public Message(int counter)
            {
                this.Counter = counter;
            }
        }

        private class MessagePrime : Event
        {
            public int Counter;

            public MessagePrime(int counter)
            {
                this.Counter = counter;
            }
        }

        private class Config : Event
        {
            public MachineId Id;
            public int NumMessages;

            public Config(MachineId id, int numMessages)
            {
                this.Id = id;
                this.NumMessages = numMessages;
            }
        }

        private class UnitTcsBool : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public UnitTcsBool(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
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

                var bid = this.CreateMachine(typeof(B1), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(bid, new Message(counter));
                }
            }
        }

        private class B1 : Machine
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
                    await this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message),
                                                    e => (e as Message).Counter == counter));
                }

                tcs.SetResult(true);
            }
        }

        private class M2 : Machine
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

                var mid = this.CreateMachine(typeof(B2), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    this.Send(mid, new Message(counter));
                    counter++;
                }
            }
        }

        private class B2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            private int NumMessages;

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
            }

            private async Task HandleMessage()
            {
                await this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message),
                                                e => (e as Message).Counter == this.NumMessages - 1));
                this.Tcs.SetResult(true);
            }
        }

        private class M3 : Machine
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

                var mid = this.CreateMachine(typeof(B3), new SetupIdEvent(this.Id, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message(counter));
                    await this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message),
                                                e => (e as Message).Counter == counter));
                }

                tcs.SetResult(true);
            }
        }

        private class B3 : Machine
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
                    await Task.WhenAny(this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message),
                                                    e => (e as Message).Counter == counter)),
                                       Task.Delay(5000));
                    this.Send(mid, new Message(counter));
                }
            }
        }

        private class M4 : Machine
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
                var bid = this.CreateMachine(typeof(B4), new Config(this.Id, numMessages));
                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(bid, new Message(counter));
                    this.Send(bid, new MessagePrime(counter));

                    await Task.WhenAny(this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message), e => (e as Message).Counter == counter),
                                       Tuple.Create<Type, Func<Event, bool>>(typeof(MessagePrime), e => (e as MessagePrime).Counter == counter)),
                               Task.Delay(5000));
                }

                tcs.SetResult(true);
            }
        }

        private class B4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var mid = (this.ReceivedEvent as Config).Id;
                var numMessages = (this.ReceivedEvent as Config).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await Task.WhenAny(this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message), e => (e as Message).Counter == counter),
                                      Tuple.Create<Type, Func<Event, bool>>(typeof(MessagePrime), e => (e as MessagePrime).Counter == counter)),
                              Task.Delay(5000));
                    this.Send(mid, new Message(counter));
                    this.Send(mid, new MessagePrime(counter));
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

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;
                var bid = this.CreateMachine(typeof(B5), new Config(this.Id, numMessages));
                var counter = 0;
                bool res = false;
                while (counter < numMessages)
                {
                    counter++;
                    // this.Logger.WriteLine("Counter in M5 is {0}", counter);
                    this.Send(bid, new Message(counter));
                    this.Send(bid, new MessagePrime(counter));

                    Event e1 = await this.Receive(typeof(Message));
                    if (e1 is Message eventMessage)
                    {
                        if (eventMessage.Counter == counter)
                        {
                            res = true;
                            // this.Logger.WriteLine("For Message: eventMessage.Counter == counter");
                            Event e2 = await this.Receive(typeof(MessagePrime));
                            if (e2 is MessagePrime eventMessagePrime)
                            {
                                res = true;
                                if (eventMessagePrime.Counter == counter)
                                {
                                    // this.Logger.WriteLine("For MessagePrime: eventMessage.Counter == counter");
                                }
                                else
                                {
                                    // this.Logger.WriteLine("For MessagePrime: eventMessage.Counter != counter");
                                    res = false;
                                }
                            }
                        }
                        else
                        {
                            // this.Logger.WriteLine("For Message: eventMessage.Counter != counter");
                            res = false;
                        }
                    }
                }

                if (res)
                {
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetResult(false);
                }
            }
        }

        private class B5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var mid = (this.ReceivedEvent as Config).Id;
                var numMessages = (this.ReceivedEvent as Config).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    // this.Logger.WriteLine("Counter in B5 is {0}", counter);
                    await Task.WhenAny(this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(Message), e => (e as Message).Counter == counter),
                                      Tuple.Create<Type, Func<Event, bool>>(typeof(MessagePrime), e => (e as MessagePrime).Counter == counter)),
                              Task.Delay(5000));
                    this.Send(mid, new Message(counter));
                    this.Send(mid, new MessagePrime(counter));
                }
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
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;
                var bid = this.CreateMachine(typeof(B6), new Config(this.Id, numMessages));
                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(bid, new Message(counter));
                    this.Send(bid, new MessagePrime(counter));

                    await Task.WhenAny(this.Receive(typeof(Message), typeof(MessagePrime)),
                               Task.Delay(5000));
                }

                tcs.SetResult(true);
            }
        }

        private class B6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var mid = (this.ReceivedEvent as Config).Id;
                var numMessages = (this.ReceivedEvent as Config).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await Task.WhenAny(this.Receive(typeof(Message), typeof(MessagePrime)),
                               Task.Delay(5000));
                    this.Send(mid, new Message(counter));
                    this.Send(mid, new MessagePrime(counter));
                }
            }
        }

        [Fact(Timeout = 15000)]
        public void TestMultipleReceivesWithPreds()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 100; i++)
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
        public void TestReceiveEventAlternateWithPreds()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 100; i++)
            {
                var test = new Action<IMachineRuntime>((r) =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M2), new SetupTcsEvent(tcs, 10000));
                    Assert.True(tcs.Task.Result);
                });

                this.Run(configuration, test);
            }
        }

        [Fact(Timeout = 15000)]
        public void TestReceiveEventExchangeWithPreds()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 100; i++)
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
        public void TestReceiveMultipleEventExchangeWithPreds()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 100; i++)
            {
                var test = new Action<IMachineRuntime>((r) =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M4), new SetupTcsEvent(tcs, 1000));
                    Assert.True(tcs.Task.Result);
                });

                this.Run(configuration, test);
            }
        }

        [Fact(Timeout = 15000)]
        public void TestReceiveMultipleEventExchangeEnclosedReceives()
        {
            var configuration = GetConfiguration();
            // configuration.IsVerbose = true;
            for (int i = 0; i < 100; i++)
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

        [Fact(Timeout = 15000)]
        public void TestReceiveMultipleEventExchangeNoPreds()
        {
            var configuration = GetConfiguration();
            for (int i = 0; i < 100; i++)
            {
                var test = new Action<IMachineRuntime>((r) =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M6), new SetupTcsEvent(tcs, 10000));
                    Assert.True(tcs.Task.Result);
                });

                this.Run(configuration, test);
            }
        }
    }
}
