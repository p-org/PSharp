// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class OnEventDequeueOrHandledTest : BaseTest
    {
        class E : Event { }

        class E1 : Event { }
        class E2 : Event { }
        class E3: Event { }

        class Begin : Event
        {
            public Event Ev;
            public Begin(Event ev)
            {
                this.Ev = ev;
            }
        }

        class End : Event
        {
            public Event Ev;
            public End(Event ev)
            {
                this.Ev = ev;
            }
        }

        class Done : Event { }

        // Ensures that machine M1 sees the following calls:
        // OnEventDequeueAsync(E1), OnEventHandledAsync(E1), OnEventDequeueAsync(E2), OnEventHandledAsync(E2)
        class Spec1 : Monitor
        {
            int counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            [OnEventDoAction(typeof(End), nameof(Process))]
            class S1 : MonitorState { }

            [Cold]
            class S2 : MonitorState { }

            void Process()
            {
                if (counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    counter++;
                }
                else if (counter == 1 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E1)
                {
                    counter ++;
                }
                else if (counter == 2 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E2)
                {
                    counter++;
                }
                else if (counter == 3 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E2)
                {
                    counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (counter == 4)
                {
                    this.Goto<S2>();
                }
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            [OnEventDoAction(typeof(E2), nameof(Process))]
            [OnEventDoAction(typeof(E3), nameof(ProcessE3))]
            class Init : MachineState { }

            void Process()
            {
                this.Raise(new E3());
            }

            void ProcessE3() { }

            protected override Task OnEventDequeueAsync(Event ev)
            {
                this.Monitor<Spec1>(new Begin(ev));
                return Task.FromResult(true);
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Monitor<Spec1>(new End(ev));
                return Task.FromResult(true);
            }
        }

        [Fact]
        public void TestOnProcessingCalled()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec1));
                var m = r.CreateMachine(typeof(M1), new E());
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2());
            });

            AssertSucceeded(test);
        }

        // Ensures that machine M2 sees the following calls:
        // OnEventDequeueAsync(E1)
        class Spec2 : Monitor
        {
            int counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            class S1 : MonitorState { }

            [Cold]
            class S2 : MonitorState { }

            void Process()
            {
                if (counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (counter == 1)
                {
                    this.Goto<S2>();
                }
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            class Init : MachineState { }

            void Process()
            {
                this.Raise(new Halt());
            }

            protected override Task OnEventDequeueAsync(Event ev)
            {
                this.Monitor<Spec2>(new Begin(ev));
                return Task.FromResult(true);
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Monitor<Spec2>(new End(ev));
                return Task.FromResult(true);
            }
        }

        [Fact]
        public void TestOnProcessingNotCalledOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec2));
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E1());
            });

            AssertSucceeded(test);
        }

        class Spec3 : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(S2))]
            class S1 : MonitorState { }

            [Cold]
            class S2 : MonitorState { }
        }

        class M3 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            class S1 : MachineState { }

            class S2 : MachineState { }

            [OnEntry(nameof(Finish))]
            class S3 : MachineState { }

            void Process()
            {
                this.Goto<S2>();
            }

            void Finish()
            {
                this.Monitor<Spec3>(new Done());
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Assert(ev is E1);
                this.Assert(this.CurrentState.Name == typeof(S2).Name);
                this.Goto<S3>();
                return Task.FromResult(true);
            }
        }

        [Fact]
        public void TestOnProcessingCanGoto()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec3));
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new E1());
            });

            AssertSucceeded(test);
        }

        class Spec4 : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(S2))]
            class S1 : MonitorState { }

            [Cold]
            class S2 : MonitorState { }
        }

        class M4 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            class S1 : MachineState { }

            void Process() { }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Raise(new Halt());
                return Task.FromResult(true);
            }
            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact]
        public void TestOnProcessingCanHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // dropped silently
            });

            AssertSucceeded(test);
        }
    }
}
