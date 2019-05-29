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
    public class PushTest : BaseTest
    {
        public PushTest(ITestOutputHelper output)
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

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private int I;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitAction))]
            [OnEventPushState(typeof(E), typeof(CallState))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(CallEntry))]
            private class CallState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.I = 0;
                this.Raise(new E());
            }

            private void ExitAction()
            {
                // Unreachable:
                // after the Call state is popped with (i == 3), the queue is empty,
                // machine keeps waiting for an event, and exit actions are never executed
                this.Tcs.SetResult(false);
            }

            private void CallEntry()
            {
                if (this.I == 3)
                {
                    this.Pop();
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.I++;
                }

                this.Raise(new E());
            }
        }

        private class M2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private int I;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitAction))]
            [OnEventPushState(typeof(E), typeof(CallState))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(CallEntry))]
            private class CallState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.I = 0;
                this.Send(this.Id, new E());
            }

            private void ExitAction()
            {
                // Unreachable:
                // after the Call state is popped with (i == 3), the queue is empty,
                // machine keeps waiting for an event, and exit actions are never executed
                this.Tcs.SetResult(false);
            }

            private void CallEntry()
            {
                if (this.I == 3)
                {
                    this.Pop();
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.I++;
                }

                this.Send(this.Id, new E());
            }
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
            {
                this.Id = id;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E2prime : Event
        {
            public int I;

            public E2prime(int i)
            {
                this.I = i;
            }
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class Unit : Event
        {
        }

        private class Real1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.GhostMachine = this.CreateMachine(typeof(Ghost1), new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.GhostMachine, new E1(), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // Reachable:
                this.Tcs.SetResult(true);
            }

            private void Action1()
            {
                this.Send(this.GhostMachine, new E3(), new SendOptions(assert: 1));
            }
        }

        private class Ghost1 : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E2(), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4(), new SendOptions(assert: 1));
            }
        }

        private class Real2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventDoAction(typeof(E2prime), nameof(Action1))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.GhostMachine = this.CreateMachine(typeof(Ghost2), new Config(this.Id));
                this.Raise(new Unit());
            }

            // Since S1 doesn't have a handler for E2prime, it pops upon
            // dequeueing E2prime, and Action1 executes in the Init state
            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.GhostMachine, new E1(), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // Reachable:
                this.Tcs.SetResult(true);
            }

            private void Action1()
            {
                int i = (this.ReceivedEvent as E2prime).I;
                if (i != 100)
                {
                     this.Tcs.SetResult(false);
                }

                this.Send(this.GhostMachine, new E3(), new SendOptions(assert: 1));
            }
        }

        private class Ghost2 : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E2prime(100), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4(), new SendOptions(assert: 1));
            }
        }

        private class M3 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool XYZ;

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
            [OnEventGotoState(typeof(E1), typeof(State))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            [OnExit(nameof(OnExitState))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
            }

            private void OnExitState()
            {
                this.Send(this.Id, new E2(), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(S1Entry))]
            private class S1 : MachineState
            {
            }

            private void S1Entry()
            {
                this.XYZ = true;
                this.Send(this.Id, new E3(), new SendOptions(assert: 1));
                // at this point, the queue is: E1; E3; S1 pops and "State" is re-entered
            }

            private void Action1()
            {
                // this.Logger.WriteLine("In Action1: XYZ = {0}", this.XYZ);
                if (this.XYZ == false)
                {
                    this.Tcs.SetResult(false);
                }
                else
                {
                    this.Tcs.SetResult(true);
                }
            }
        }

        private class M4 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool XYZ;

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
            [OnEventPushState(typeof(E1), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            [OnExit(nameof(OnExitState))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
            }

            private void OnExitState()
            {
                // Unreachable:
                this.Send(this.Id, new E2(), new SendOptions(assert: 1));
                this.Tcs.SetResult(false);
            }

            [OnEntry(nameof(S1Entry))]
            private class S1 : MachineState
            {
            }

            private void S1Entry()
            {
                this.XYZ = true;
                this.Send(this.Id, new E3(), new SendOptions(assert: 1));
                this.Pop();
            }

            private void Action1()
            {
                this.Logger.WriteLine("In Action1: XYZ = {0}", this.XYZ);
                if (this.XYZ == false)
                {
                    this.Tcs.SetResult(false);
                }
                else
                {
                    this.Tcs.SetResult(true);
                }
            }
        }

        private class M5 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool XYZ;

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
            [OnEventPushState(typeof(E1), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            [OnExit(nameof(OnExitState))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
            }

            private void OnExitState()
            {
                // Unreachable:
                this.Send(this.Id, new E2(), new SendOptions(assert: 1));
                this.Tcs.SetResult(false);
            }

            [OnEntry(nameof(S1Entry))]
            private class S1 : MachineState
            {
            }

            private void S1Entry()
            {
                this.XYZ = true;
                this.Send(this.Id, new E3(), new SendOptions(assert: 1));
            }

            private void Action1()
            {
                this.Logger.WriteLine("In Action1: XYZ = {0}", this.XYZ);
                if (this.XYZ == false)
                {
                    this.Tcs.SetResult(false);
                }
                else
                {
                    this.Tcs.SetResult(true);
                }
            }
        }

        private class M6 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool XYZ;

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
        [OnEventPushState(typeof(E2), typeof(S1))]
        [OnEventGotoState(typeof(E1), typeof(State))]
        [OnEventDoAction(typeof(E3), nameof(Action1))]
        [OnExit(nameof(OnExitState))]
        private class State : MachineState
        {
        }

        private void StateEntry()
        {
            this.Send(this.Id, new E1(), new SendOptions(assert: 1));
        }

        private void OnExitState()
        {
            this.Send(this.Id, new E2(), new SendOptions(assert: 1));
        }

        [OnEntry(nameof(S1Entry))]
        [OnExit(nameof(S1Exit))]
        private class S1 : MachineState
        {
        }

        private void S1Entry()
        {
            this.XYZ = true;
            this.Send(this.Id, new E3(), new SendOptions(assert: 1));
        }

        private void S1Exit()
        {
                // Reachable:
                this.Tcs.SetResult(true);
        }

        private void Action1()
        {
        }
    }

        private class M7 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool XYZ;

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
            [OnEventPushState(typeof(E1), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            [OnExit(nameof(OnExitState))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
            }

            private void OnExitState()
            {
                this.Send(this.Id, new E2(), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(S1Entry))]
            [OnExit(nameof(S1Exit))]
            private class S1 : MachineState
            {
            }

            private void S1Entry()
            {
                this.XYZ = true;
                // this.Send(this.Id, new E3(), new SendOptions(assert: 1));
                this.Pop();
            }

            private void S1Exit()
            {
                // Reachable:
                this.Tcs.SetResult(true);
            }

            private void Action1()
            {
            }
        }

        private class M8 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private int Count;

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
            [OnEventPushState(typeof(E2), typeof(S1))]
            [OnEventGotoState(typeof(E1), typeof(State))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            [OnExit(nameof(OnExitState))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Count++;
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
                if (this.Count == 3)
                {
                    // By this time, there are two E1 events in the queue -
                    // hence, there should be an error reported:
                    // "Attempting to enqueue event ____E1 more than max instance of 1"
                    // This does not happen.
                    this.Tcs.SetResult(true);
                }
            }

            private void OnExitState()
            {
                this.Send(this.Id, new E2(), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(S1Entry))]
            private class S1 : MachineState
            {
            }

            private void S1Entry()
            {
                this.Raise(new E1());
            }

            private void Action1()
            {
                // Unreachable:
                this.Tcs.SetResult(false);
            }
        }

        [Fact(Timeout=5000)]
        // Similar to \P\Tst\RegressionTests\Feature1SMLevelDecls\Correct\AlonBug\AlonBug.p
        public void PushTest1()
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
        // Similar to \P\Tst\RegressionTests\Feature1SMLevelDecls\Correct\AlonBug\AlonBug.p,
        // but Send instead of Raise
        public void PushTest2()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M2), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\Actions_5\Actions_5.p
        public void PushTest3()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(Real1), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\Actions_6\Actions_6.p
        public void PushTest4()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(Real2), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_11\PushImplicitPopWithSend.p
        public void PushTest5()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M3), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_12\PushExplicitPop.p
        public void PushTest6()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M4), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_13\PushTransInheritance.p
        public void PushTest7()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M5), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_15\ImplicitPopExit.p
        public void PushTest8()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M6), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_16\ExplicitPopExit.p
        public void PushTest9()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M7), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        // [Fact(Timeout = 5000)]
        // Similar to: \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_17\PushImplicitPopWithRaise.p
        // TODO: this test does not work as it does in P.
        // Possible issue: incorrect handling of the max instances of an event in a queue
        public void PushTest10()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M8), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }
    }
}
