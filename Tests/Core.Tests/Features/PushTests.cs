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
        // \P\Tst\RegressionTests\Integration\DynamicError\Actions_5\Actions_5.p
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
    }
}
