// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class OnEventDroppedTest
    {
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
            var runtime = PSharpRuntime.Create();
            var called = false;
            var tcs = new TaskCompletionSource<bool>();

            runtime.OnEventDropped += delegate (Event e, MachineId target)
            {
                called = true;
                tcs.SetResult(true);
            };

            var m = runtime.CreateMachine(typeof(M1));
            runtime.SendEvent(m, new Halt());

            tcs.Task.Wait(5000);
            Assert.True(called);
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
            var runtime = PSharpRuntime.Create();
            var called = false;
            var tcs = new TaskCompletionSource<bool>();

            runtime.OnEventDropped += delegate (Event e, MachineId target)
            {
                called = true;
                tcs.SetResult(true);
            };

            var m = runtime.CreateMachine(typeof(M2));

            tcs.Task.Wait(5000);
            Assert.True(called);
        }

        [Fact]
        public void TestOnDroppedParams()
        {
            var runtime = PSharpRuntime.Create();
            var called = false;
            var tcs = new TaskCompletionSource<bool>();

            var m = runtime.CreateMachine(typeof(M1));

            runtime.OnEventDropped += delegate (Event e, MachineId target)
            {
                Assert.True(e is E);
                Assert.True(target == m);
                called = true;
                tcs.SetResult(true);
            };

            runtime.SendEvent(m, new Halt());

            tcs.Task.Wait(5000);
            Assert.True(called);
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
            var config = Configuration.Create();
            config.EnableMonitorsInProduction = true;

            var runtime = PSharpRuntime.Create(config);
            var tcs = new TaskCompletionSource<bool>();

            runtime.RegisterMonitor(typeof(Monitor3));
            runtime.InvokeMonitor(typeof(Monitor3), new E(tcs));

            runtime.OnFailure += delegate
            {
                Assert.True(false);
                tcs.SetResult(false);
            };

            runtime.OnEventDropped += delegate (Event e, MachineId target)
            {
                runtime.InvokeMonitor(typeof(Monitor3), new EventDropped());
            };

            var m = runtime.CreateMachine(typeof(M3c));
            runtime.CreateMachine(typeof(M3a), new E(m));
            runtime.CreateMachine(typeof(M3b), new E(m));
            tcs.Task.Wait(5000);
        }
    }
}
