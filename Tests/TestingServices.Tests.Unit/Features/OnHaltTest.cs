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
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class OnHaltTest : BaseTest
    {
        public OnHaltTest(ITestOutputHelper output)
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
            MachineId sender;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                sender = (this.ReceivedEvent as E).Id;
                this.Raise(new Halt());
            }

            protected override async Task OnHaltAsync()
            {
                // no-ops but no failure
                await this.SendAsync(sender, new E());
                this.Random();
                this.Assert(true);
                await this.CreateMachineAsync(typeof(Dummy));
            }
        }

        class M4 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }
        }

        [Fact]
        public void TestHaltCalled()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestReceiveOnHalt()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2a));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2a()' invoked Receive while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestRaiseOnHalt()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2b));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2b()' invoked Raise while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoOnHalt()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2c));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2c()' invoked Goto while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestAPIsOnHalt()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M4));
                r.CreateMachine(typeof(M3), new E(m));
            });

            AssertSucceeded(test);
        }
    }
}
