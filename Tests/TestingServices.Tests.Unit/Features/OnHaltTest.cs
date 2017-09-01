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

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class OnHaltTest : BaseTest
    {
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
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                // no-ops but no failure
                this.Send(this.Id, new E());
                this.Random();
                this.Assert(true);
                this.CreateMachine(typeof(Dummy));
            }
        }

        [Fact]
        public void TestHaltCalled()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestReceiveOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2a));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2a()' invoked Receive while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestRaiseOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2b));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2b()' invoked Raise while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2c));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2c()' invoked Goto while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestAPIsOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            AssertSucceeded(test);
        }
    }
}
