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
    public class HaltTest : BaseTest
    {
        public HaltTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class Unit : Event
        {
        }

        private class E1 : Event
        {
        }

        private class M1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool boolResult;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(State))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(StateEntry))]
            [OnEventPushState(typeof(Halt), typeof(State1))]
            [OnEventDoAction(typeof(E1), nameof(Action2))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
                this.Raise(new Halt());
            }

            private void Action2()
            {
                if (this.boolResult)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Tcs.SetResult(false);
                }
            }

            [OnEntry(nameof(State1Entry))]
            private class State1 : MachineState
            {
            }

            private void State1Entry()
            {
                this.boolResult = true;
            }
        }

        internal class Ping : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public MachineId Machine;

            public Ping(TaskCompletionSource<bool> tcs, MachineId machine)
            {
                this.Tcs = tcs;
                this.Machine = machine;
            }
        }

        private class PongEvt : Event
        {
        }

        private class Success : Event
        {
        }

        private class PingIgnored : Event
        {
        }

        private class PongHalted : Event
        {
        }

        private class Main1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private int Count;
            private MachineId PongId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(State))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(StateEntry))]
            [OnEventGotoState(typeof(Success), typeof(Ping_SendPing))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.PongId = this.CreateMachine(typeof(Pong1), new SetupEvent(this.Tcs));
                this.Raise(new Success());
            }

            [OnEntry(nameof(Ping_SendPingEntry))]
            [OnEventGotoState(typeof(Success), typeof(Ping_WaitPong))]
            private class Ping_SendPing : MachineState
            {
            }

            private void Ping_SendPingEntry()
            {
                this.Count++;
                if (this.Count == 1)
                {
                    this.Send(this.PongId, new Ping(this.Tcs, this.Id), new SendOptions(assert: 1));
                }

                // Halt Pong machine after one exchange:
                if (this.Count == 2)
                {
                    this.Logger.WriteLine("Count == 2");
                    this.Send(this.PongId, new PingIgnored());
                }

                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(PongEvt), typeof(Ping_SendPing))]
            private class Ping_WaitPong : MachineState
            {
            }

            private class Done : MachineState
            {
            }
        }

        private class Pong1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Pong_WaitPing))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(Ping), typeof(Pong_SendPong))]
            [OnEventDoAction(typeof(PingIgnored), nameof(Action))]
            private class Pong_WaitPing : MachineState
            {
            }

            [OnEntry(nameof(Pong_SendPongEntry))]
            [OnEventGotoState(typeof(Success), typeof(Pong_WaitPing))]
            private class Pong_SendPong : MachineState
            {
            }

            private void Pong_SendPongEntry()
            {
                MachineId payload = (this.ReceivedEvent as Ping).Machine;
                this.Send(payload, new PongEvt(), new SendOptions(assert: 1));
                this.Raise(new Success());
            }

            private void Action()
            {
                    this.Tcs.SetResult(true);
            }
        }

        private class Main2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private int Count;
            private MachineId PongId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(State))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(StateEntry))]
            [OnEventGotoState(typeof(Success), typeof(Ping_SendPing))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.PongId = this.CreateMachine(typeof(Pong2), new SetupEvent(this.Tcs));
                this.Raise(new Success());
            }

            [OnEntry(nameof(Ping_SendPingEntry))]
            [OnEventGotoState(typeof(Success), typeof(Ping_WaitPong))]
            private class Ping_SendPing : MachineState
            {
            }

            private void Ping_SendPingEntry()
            {
                this.Count++;
                if (this.Count == 1)
                {
                    this.Send(this.PongId, new Ping(this.Tcs, this.Id), new SendOptions(assert: 1));
                }

                // Halt Pong machine after one exchange:
                if (this.Count == 2)
                {
                    this.Send(this.PongId, new Halt());
                    this.Logger.WriteLine("Count == 2");
                    this.Send(this.PongId, new PingIgnored());
                }

                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(PongEvt), typeof(Ping_SendPing))]
            private class Ping_WaitPong : MachineState
            {
            }

            private class Done : MachineState
            {
            }
        }

        private class Pong2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Pong_WaitPing))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(Ping), typeof(Pong_SendPong))]
            [OnEventGotoState(typeof(Halt), typeof(Pong_Halt))]
            private class Pong_WaitPing : MachineState
            {
            }

            [OnEntry(nameof(Pong_SendPongEntry))]
            [OnEventGotoState(typeof(Success), typeof(Pong_WaitPing))]
            private class Pong_SendPong : MachineState
            {
            }

            private void Pong_SendPongEntry()
            {
                MachineId payload = (this.ReceivedEvent as Ping).Machine;
                this.Send(payload, new PongEvt(), new SendOptions(assert: 1));
                this.Raise(new Success());
            }

            [IgnoreEvents(typeof(Ping))]
            [OnEventDoAction(typeof(PingIgnored), nameof(Action))]
            private class Pong_Halt : MachineState
            {
            }

            private void Action()
            {
                this.Tcs.SetResult(true);
            }
        }

        private class Main3 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private MachineId PongId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(State))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(StateEntry))]
            [OnEventGotoState(typeof(Success), typeof(Ping_SendPing))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.PongId = this.CreateMachine(typeof(Pong3), new SetupEvent(this.Tcs));
                this.Raise(new Success());
            }

            [OnEntry(nameof(Ping_SendPingEntry))]
            [OnEventGotoState(typeof(Success), typeof(Ping_WaitPong))]
            private class Ping_SendPing : MachineState
            {
            }

            private void Ping_SendPingEntry()
            {
                this.Send(this.PongId, new Ping(this.Tcs, this.Id), new SendOptions(assert: 1));
                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(PongEvt), typeof(Ping_SendPing))]
            private class Ping_WaitPong : MachineState
            {
            }

            private class Done : MachineState
            {
            }
        }

        private class Pong3 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Pong_WaitPing))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(Ping), typeof(Pong_SendPong))]
            private class Pong_WaitPing : MachineState
            {
            }

            [OnEntry(nameof(Pong_SendPongEntry))]
            [OnEventDoAction(typeof(Halt), nameof(Action))]
            [OnEventGotoState(typeof(Success), typeof(Pong_WaitPing))]
            private class Pong_SendPong : MachineState
            {
            }

            private void Pong_SendPongEntry()
            {
                this.Count++;
                MachineId payload = (this.ReceivedEvent as Ping).Machine;
                if (this.Count == 1)
                {
                    this.Send(payload, new PongEvt(), new SendOptions(assert: 1));
                    this.Raise(new Success());
                }
                else if (this.Count == 2)
                {
                    this.Logger.WriteLine("Count == 2");
                    this.Send(payload, new PongEvt(), new SendOptions(assert: 1));
                    this.Raise(new Halt());
                }
                else
                {
                    // this.Logger.WriteLine("Count == {0}", this.Count);
                    // Unreachable:
                    this.Tcs.SetResult(false);
                }
            }

            private void Action()
            {
                this.Tcs.SetResult(true);
            }
        }

        [Fact(Timeout=5000)]
        // Similar to \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_32\RaisedHaltHandled.p
        public void RaisedHaltHandled()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Integration\DynamicError\SEM_TwoMachines_2\EventSentAfterSentHalt_v.p
        public void EventSentAfterSentHalt()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(Main1), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Integration\DynamicError\SEM_TwoMachines_4\EventSentAfterSentHaltHandled_v.p
        public void EventSentAfterSentHaltPrime()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(Main2), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Integration\DynamicError\SEM_TwoMachines_6\RaisedHaltHandled.p
        public void RaisedHaltHandledPingPong()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(Main3), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }
    }
}
