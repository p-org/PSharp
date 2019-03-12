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
    public class OnHaltTest : BaseTest
    {
        public OnHaltTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public MachineId Id;
            public TaskCompletionSource<bool> tcs;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
            public E(TaskCompletionSource<bool> tcs)
            {
                this.tcs = tcs;
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Assert(false);
            }
        }

        class M2a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Receive(typeof(Event)).Wait();
            }
        }

        class M2b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Raise(new E());
            }
        }

        class M2c : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Goto<Init>();
            }
        }

        class Dummy : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        class M3 : Machine
        {
            TaskCompletionSource<bool> tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                tcs = (this.ReceivedEvent as E).tcs;
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                // no-ops but no failure
                this.Send(this.Id, new E());
                this.Random();
                this.Assert(true);
                this.CreateMachine(typeof(Dummy));

                tcs.TrySetResult(true);
            }
        }

        private void AssertSucceeded(Type machine)
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
            });

            base.Run(config, test);

            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.TrySetResult(true);
            };

            runtime.CreateMachine(machine, new E(tcs));

            tcs.Task.Wait(5000);
            Assert.False(failed);
        }

        private void AssertFailed(Type machine)
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateMachine(machine);

                tcs.Task.Wait(5000);
                Assert.True(failed);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestHaltCalled()
        {
            AssertFailed(typeof(M1));
        }

        [Fact]
        public void TestReceiveOnHalt()
        {
            AssertFailed(typeof(M2a));
        }

        [Fact]
        public void TestRaiseOnHalt()
        {
            AssertFailed(typeof(M2b));
        }

        [Fact]
        public void TestGotoOnHalt()
        {
            AssertFailed(typeof(M2c));
        }

        [Fact]
        public void TestAPIsOnHalt()
        {
            AssertSucceeded(typeof(M3));
        }
    }
}
