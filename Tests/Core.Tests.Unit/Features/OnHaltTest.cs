// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
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

            protected override Task OnHaltAsync()
            {
                this.Assert(false);
#if NET45
                return Task.FromResult(0);
#else
                return Task.CompletedTask;
#endif
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

            protected override async Task OnHaltAsync()
            {
                await this.Receive(typeof(Event));
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

            protected override Task OnHaltAsync()
            {
                this.Raise(new E());
#if NET45
                return Task.FromResult(0);
#else
                return Task.CompletedTask;
#endif
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

            protected override Task OnHaltAsync()
            {
                this.Goto<Init>();
#if NET45
                return Task.FromResult(0);
#else
                return Task.CompletedTask;
#endif
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

            protected override async Task OnHaltAsync()
            {
                // no-ops but no failure
                await this.SendAsync(this.Id, new E());
                this.Random();
                this.Assert(true);
                await this.CreateMachineAsync(typeof(Dummy));

                tcs.TrySetResult(true);
            }
        }

        private void AssertSucceeded(Type machine)
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
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
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            runtime.CreateMachine(machine);

            tcs.Task.Wait(5000);
            Assert.True(failed);
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
