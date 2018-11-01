// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class OnEventDroppedTest : BaseTest
    {
        public OnEventDroppedTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public MachineId Id;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
        }

        class M1 : Machine
        {
            [Start]
            class Init : MachineState { }

            protected override void OnHalt()
            {
                this.Send(this.Id, new E());
            }
        }

        [Fact]
        public void TestOnDroppedCalled1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    r.Assert(false);
                };

                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new Halt());
            });

            AssertFailed(test, 1, true);
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new Halt());
                this.Send(this.Id, new E());
            }
        }

        [Fact]
        public void TestOnDroppedCalled2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    r.Assert(false);
                };

                var m = r.CreateMachine(typeof(M2));
            });

            AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOnDroppedParams()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M1));

                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    r.Assert(e is E);
                    r.Assert(target == m);
                };

                r.SendEvent(m, new Halt());
            });

            AssertSucceeded(test);
        }

        class EventProcessed : Event { }
        class EventDropped : Event { }

        class Monitor3 : Monitor
        {
            [Hot]
            [Start]
            [OnEventGotoState(typeof(EventProcessed), typeof(S2))]
            [OnEventGotoState(typeof(EventDropped), typeof(S2))]
            class S1 : MonitorState { }

            [Cold]
            class S2 : MonitorState { }
        }

        class M3a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send((this.ReceivedEvent as E).Id, new Halt());
            }
        }

        class M3b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send((this.ReceivedEvent as E).Id, new E());
            }
        }

        class M3c : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Processed))]
            class Init : MachineState { }

            void Processed()
            {
                this.Monitor<Monitor3>(new EventProcessed());
            }
        }

        [Fact]
        public void TestProcessedOrDropped()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Monitor3));
                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    r.InvokeMonitor(typeof(Monitor3), new EventDropped());
                };

                var m = r.CreateMachine(typeof(M3c));
                r.CreateMachine(typeof(M3a), new E(m));
                r.CreateMachine(typeof(M3b), new E(m));
            });

            var config = Configuration.Create().WithNumberOfIterations(1000);
            AssertSucceeded(config, test);
        }
    }
}
