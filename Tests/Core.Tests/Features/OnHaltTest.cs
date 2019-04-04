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
        {
        }

        private class E : Event
        {
            public MachineId Id;
            public TaskCompletionSource<bool> Tcs;

            public E()
            {
            }

            public E(MachineId id)
            {
                this.Id = id;
            }

            public E(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
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
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Assert(false);
            }
        }

        private class M2a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Receive(typeof(Event)).Wait();
            }
        }

        private class M2b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Raise(new E());
            }
        }

        private class M2c : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Goto<Init>();
            }
        }

        private class Dummy : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        private class M3 : Machine
        {
            private TaskCompletionSource<bool> tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.tcs = (this.ReceivedEvent as E).Tcs;
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                // no-ops but no failure
                this.Send(this.Id, new E());
                this.Random();
                this.Assert(true);
                this.CreateMachine(typeof(Dummy));

                this.tcs.TrySetResult(true);
            }
        }

        private void AssertSucceeded(Type machine)
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
            });

            this.Run(config, test);

            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += (ex) =>
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
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateMachine(machine);

                tcs.Task.Wait(5000);
                Assert.True(failed);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestHaltCalled()
        {
            this.AssertFailed(typeof(M1));
        }

        [Fact]
        public void TestReceiveOnHalt()
        {
            this.AssertFailed(typeof(M2a));
        }

        [Fact]
        public void TestRaiseOnHalt()
        {
            this.AssertFailed(typeof(M2b));
        }

        [Fact]
        public void TestGotoOnHalt()
        {
            this.AssertFailed(typeof(M2c));
        }

        [Fact]
        public void TestAPIsOnHalt()
        {
            this.AssertSucceeded(typeof(M3));
        }
    }
}
