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
    public class GotoTest : BaseTest
    {
        public GotoTest(ITestOutputHelper output)
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
            [OnEventGotoState(typeof(E), typeof(CallState))]
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
                this.Tcs.SetResult(true);
            }

            private void CallEntry()
            {
                this.Logger.WriteLine("CallEntry: I = {0}", this.I);
                if (this.I == 3)
                {
                    this.Pop();
                    this.Logger.WriteLine("CallEntry: after Pop");
                    // this.Tcs.SetResult(true);
                }
                else
                {
                    this.I++;
                }

                this.Logger.WriteLine("CallEntry: before Raise");
                this.Raise(new E());
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
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E2), typeof(S1))] // exit actions are performed before transition to S1
            [OnEventDoAction(typeof(E4), nameof(Action1))] // E4, E3 have no effect on reachability of assert(false)
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.GhostMachine = this.CreateMachine(typeof(Ghost1), new Config(this.Id));
                this.Send(this.GhostMachine, new E1(), new SendOptions(assert: 1));
            }

            private void ExitInit()
            {
                this.Test = true;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                if (this.Test == false)
                {
                    this.Tcs.SetResult(false);
                }

                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // Reachable: Real -E1-> Ghost -E2-> Real;
                // then Real_S1 (assert holds), Real_S2 (assert fails)
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
            // [OnEventDoAction(typeof(Config), nameof(Configure))]
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
                this.Send(this.RealMachine, new E4(), new SendOptions(assert: 1));
                this.Send(this.RealMachine, new E2(), new SendOptions(assert: 1));
            }

            private class S2 : MachineState
            {
            }
        }

        private class Real2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))] // exit actions are performed before transition to S2
            [OnEventDoAction(typeof(E2), nameof(Action1))] // E4, E3 have no effect on reachability of assert(false)
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.GhostMachine = this.CreateMachine(typeof(Ghost2), new Config(this.Id));
                this.Send(this.GhostMachine, new E1(), new SendOptions(assert: 1));
            }

            private void EntryS1()
            {
                this.Raise(new Unit());
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

        [Fact(Timeout = 5000)]
        // \P\Tst\RegressionTests\Feature1SMLevelDecls\DynamicError\AlonBug_fails
        // Compare this test with Core.Tests.PushTest.PushTest1
        public void GotoTest1()
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
        // \P\Tst\RegressionTests\Integration\DynamicError\Actions_1\Actions_1.p
        public void GotoTest2()
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
        // \P\Tst\RegressionTests\Integration\DynamicError\Actions_3\Actions_3.p
        public void GotoTest3()
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
    }
}
