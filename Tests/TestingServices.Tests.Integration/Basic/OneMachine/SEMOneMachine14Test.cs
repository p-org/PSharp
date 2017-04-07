//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine14Test.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine14Test : BaseTest
    {
        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public E2() : base(1, -1) { }
        }

        class E3 : Event
        {
            public E3() : base(1, -1) { }
        }

        class Real1 : Machine
        {
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E1());
            }

            [OnEntry(nameof(EntryS1))]
            [OnExit(nameof(ExitS1))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            class S1 : MachineState { }

            void EntryS1()
            {
                test = true;
                this.Send(this.Id, new E3());
            }

            void ExitS1()
            {
                this.Send(this.Id, new E3());
            }

            [OnEntry(nameof(EntryS2))]
            class S2 : MachineState { }

            void EntryS2()
            {
                this.Assert(test == false); // reachable
            }
        }
        
        /// <summary>
        /// P# semantics test: one machine, "goto" transition, action is not inherited
        /// by the destination state. This test checks that after "goto" transition,
        /// action of the src state is not inherited by the destination state.
        /// </summary>
        [Fact]
        public void TestGotoTransInheritance()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
