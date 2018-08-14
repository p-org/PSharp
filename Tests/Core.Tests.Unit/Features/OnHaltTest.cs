//-----------------------------------------------------------------------
// <copyright file="OnHaltTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class OnHaltTest
    {
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
