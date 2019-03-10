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
    public class OnEventDroppedTest : BaseTest
    {
        public OnEventDroppedTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public MachineId Id;
            public TaskCompletionSource<bool> Tcs;

            public E() { }

            public E(MachineId id)
            {
                this.Id = id;
            }

            public E(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
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
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    called = true;
                    tcs.SetResult(true);
                };

                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new Halt());

                tcs.Task.Wait(5000);
                Assert.True(called);
            });

            base.Run(config, test);
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
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    called = true;
                    tcs.SetResult(true);
                };

                var m = r.CreateMachine(typeof(M2));

                tcs.Task.Wait(5000);
                Assert.True(called);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestOnDroppedParams()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                var m = r.CreateMachine(typeof(M1));

                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    Assert.True(e is E);
                    Assert.True(target == m);
                    called = true;
                    tcs.SetResult(true);
                };

                r.SendEvent(m, new Halt());

                tcs.Task.Wait(5000);
                Assert.True(called);
            });

            base.Run(config, test);
        }

        class EventProcessed : Event { }
        class EventDropped : Event { }

        class Monitor3 : Monitor
        {
            TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEventDoAction(typeof(E), nameof(InitOnEntry))]
            class S0 : MonitorState { }

            void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as E).Tcs;
                this.Goto<S1>();
            }

            [OnEventGotoState(typeof(EventProcessed), typeof(S2))]
            [OnEventGotoState(typeof(EventDropped), typeof(S2))]
            class S1 : MonitorState { }

            [OnEntry(nameof(Done))]
            class S2 : MonitorState { }

            void Done()
            {
                this.Tcs.SetResult(true);
            }
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
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            config.EnableMonitorsInProduction = true;

            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();

                r.RegisterMonitor(typeof(Monitor3));
                r.InvokeMonitor(typeof(Monitor3), new E(tcs));

                r.OnFailure += delegate
                {
                    Assert.True(false);
                    tcs.SetResult(false);
                };

                r.OnEventDropped += delegate (Event e, MachineId target)
                {
                    r.InvokeMonitor(typeof(Monitor3), new EventDropped());
                };

                var m = r.CreateMachine(typeof(M3c));
                r.CreateMachine(typeof(M3a), new E(m));
                r.CreateMachine(typeof(M3b), new E(m));
                tcs.Task.Wait(5000);
            });

            base.Run(config, test);
        }
    }
}
