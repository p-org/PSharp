// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class ReceiveTestsFromP : BaseTest
    {
        public ReceiveTestsFromP(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class G : Event
        {
            public int I;

            public G(int i)
            {
                this.I = i;
            }
        }

        private class F : Event
        {
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
            {
                this.Id = id;
            }
        }

        private class Unit : Event
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
                var bid = this.CreateMachine(typeof(B1));
                this.Send(bid, new F());
            }
        }

        private class B1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(X))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Y))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryY))]
            private class Y : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            private async Task InitOnEntryX()
            {
                this.Assert(false);
                await this.Receive(typeof(E));
                this.Assert(true);
            }

            private void InitOnEntryY()
            {
                // Since Receive in state X is blocking, event F
                // will never get dequeued, and InitOnEntryY handler is unreachable:
                this.Assert(true);
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
                var bid = this.CreateMachine(typeof(B2));
                this.Send(bid, new F());
            }
        }

        private class B2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(Unit), typeof(X))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Y))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryY))]
            private class Y : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            private async Task InitOnEntryX()
            {
                await this.Receive(typeof(F));
                this.Pop();
                this.Assert(false);
            }

            private void InitOnEntryY()
            {
                // Since receive statement ignores the event handlers of the enclosing context,
                // InitOnEntryY handler is unreachable:
                this.Assert(true);
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
                var bid = this.CreateMachine(typeof(B3));
                this.Send(bid, new F());
            }
        }

        private class B3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(Unit), typeof(X))]
            [OnEventDoAction(typeof(F), nameof(OnFDoAction))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Y))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryY))]
            private class Y : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            private async Task InitOnEntryX()
            {
                await this.Receive(typeof(F));
                this.Pop();
                this.Assert(false);
            }

            private void InitOnEntryY()
            {
                // Since receive statement ignores the event handlers of the enclosing context,
                // InitOnEntryY handler is unreachable:
                this.Assert(true);
            }

            private void OnFDoAction()
            {
                // Unreachable:
                this.Assert(true);
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(T))]
            [OnExit(nameof(ExitInit))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryT))]
            private class T : MachineState
            {
            }

            private void InitOnEntry()
            {
                var bid = this.CreateMachine(typeof(B4), new Config(this.Id));
                this.Raise(new Unit());
            }

            private async Task InitOnEntryT()
            {
                await this.Receive(typeof(G));
                this.Assert(false);
            }

            private async Task ExitInit()
            {
                await this.Receive(typeof(G));
                this.Assert(false);
            }
        }

        private class B4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                MachineId mid = (this.ReceivedEvent as Config).Id;
                this.Send(mid, new G(0));
                this.Send(mid, new G(0));
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
                var bid = this.CreateMachine(typeof(B5));
                this.Send(bid, new G(0));
                this.Send(bid, new G(0));
            }
        }

        private class B5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(Unit), typeof(T))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryT))]
            private class T : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            private async Task ExitInit()
            {
                await this.Receive(typeof(G));
                this.Assert(false);
            }

            private async Task InitOnEntryT()
            {
                await this.Receive(typeof(G));
                this.Assert(false);
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var bid = this.CreateMachine(typeof(B6));
                this.Send(bid, new G(0));
                this.Send(bid, new G(0));
            }
        }

        private class B6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(DoReceive))]
            [OnEventGotoState(typeof(Unit), typeof(T))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(DoReceive))]
            private class T : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            private async Task DoReceive()
            {
                await this.Receive(typeof(G));
                this.Assert(false);
            }
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive6\receive6.p
        public void TestReceiveEventBlocking1()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive12\receive12.p
        public void TestReceiveEventBlocking2()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive13\receive13.p
        public void TestReceiveEventBlocking3()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive14\receiveInExit1.p
        public void TestReceiveEventInExit1()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M4));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive15\receiveInExit2.p
        public void TestReceiveEventInExit2()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive16\receiveInExit3.p
        public void TestReceiveEventInExit3()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M6));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }
    }
}
