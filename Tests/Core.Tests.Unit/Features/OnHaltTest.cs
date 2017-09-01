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

                tcs.SetResult(true);
            }
        }

        public void AssertSucceeded(Type machine)
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            runtime.CreateMachine(machine, new E(tcs));

            tcs.Task.Wait(100);
            Assert.False(failed);
        }

        public void AssertFailed(Type machine)
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            runtime.CreateMachine(machine);

            tcs.Task.Wait(100);
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
