//-----------------------------------------------------------------------
// <copyright file="PushStateTest.cs">
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

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class PushApiTest : BaseTest
    {
        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            class Done : MachineState { }

            void EntryDone()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }
        }

        class E : Event { }

        class M2 : Machine
        {
            int cnt = 0;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Assert(cnt == 0); // called once
                cnt++;

                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            class Done : MachineState { }

            void EntryDone()
            {
                this.Pop();
            }
        }

        class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Push<Done>();
            }

            void ExitInit()
            {
                // This assert is not reachable.
                this.Assert(false, "Bug found.");
            }

            class Done : MachineState { }
        }

        class M4a : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                // Added a different failure mode here; try to Goto a state from another machine.
                this.Push<M4b.Done>();
            }

            class Done : MachineState { }
        }

        class M4b : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
            }

            internal class Done : MachineState { }
        }


        [Fact]
        public void TestPushSimple()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestPushPopSimple()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestPushStateExit()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            base.AssertSucceeded(test);
        }


        [Fact]
        public void TestPushBadStateFail()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M4a));
            });

            base.AssertFailed(test, 1, true);
        }
    }
}
