﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OnEventDequeueOrHandledTest : BaseTest
    {
        public OnEventDequeueOrHandledTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
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

        private class Begin : Event
        {
            public Event Ev;

            public Begin(Event ev)
            {
                this.Ev = ev;
            }
        }

        private class End : Event
        {
            public Event Ev;

            public End(Event ev)
            {
                this.Ev = ev;
            }
        }

        private class Done : Event
        {
        }

        // Ensures that machine M1 sees the following calls:
        // OnEventDequeueAsync(E1), OnEventHandledAsync(E1), OnEventDequeueAsync(E2), OnEventHandledAsync(E2)
        private class Spec1 : Monitor
        {
            private int counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            [OnEventDoAction(typeof(End), nameof(Process))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
            {
            }

            private void Process()
            {
                if (this.counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    this.counter++;
                }
                else if (this.counter == 1 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E1)
                {
                    this.counter++;
                }
                else if (this.counter == 2 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E2)
                {
                    this.counter++;
                }
                else if (this.counter == 3 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E2)
                {
                    this.counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.counter == 4)
                {
                    this.Goto<S2>();
                }
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            [OnEventDoAction(typeof(E2), nameof(Process))]
            [OnEventDoAction(typeof(E3), nameof(ProcessE3))]
            private class Init : MachineState
            {
            }

            private void Process()
            {
                this.Raise(new E3());
            }

            private void ProcessE3()
            {
            }

            protected override Task OnEventDequeueAsync(Event ev)
            {
                this.Monitor<Spec1>(new Begin(ev));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Monitor<Spec1>(new End(ev));
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void TestOnProcessingCalled()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec1));
                var m = r.CreateMachine(typeof(M1), new E());
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2());
            });

            this.AssertSucceeded(test);
        }

        // Ensures that machine M2 sees the following calls:
        // OnEventDequeueAsync(E1)
        private class Spec2 : Monitor
        {
            private int counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
            {
            }

            private void Process()
            {
                if (this.counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    this.counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.counter == 1)
                {
                    this.Goto<S2>();
                }
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class Init : MachineState
            {
            }

            private void Process()
            {
                this.Raise(new Halt());
            }

            protected override Task OnEventDequeueAsync(Event ev)
            {
                this.Monitor<Spec2>(new Begin(ev));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Monitor<Spec2>(new End(ev));
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void TestOnProcessingNotCalledOnHalt()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec2));
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E1());
            });

            this.AssertSucceeded(test);
        }

        private class Spec3 : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
            {
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }

            [OnEntry(nameof(Finish))]
            private class S3 : MachineState
            {
            }

            private void Process()
            {
                this.Goto<S2>();
            }

            private void Finish()
            {
                this.Monitor<Spec3>(new Done());
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Assert(ev is E1);
                this.Assert(this.CurrentState.Name == typeof(S2).Name);
                this.Goto<S3>();
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void TestOnProcessingCanGoto()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec3));
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new E1());
            });

            this.AssertSucceeded(test);
        }

        private class Spec4 : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
            {
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : MachineState
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Raise(new Halt());
                return Task.CompletedTask;
            }

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact]
        public void TestOnProcessingCanHalt()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // dropped silently
            });

            this.AssertSucceeded(test);
        }
    }
}
