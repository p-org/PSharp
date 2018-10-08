//-----------------------------------------------------------------------
// <copyright file="NameofTest.cs">
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
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class NameofTest : BaseTest
    {
        public NameofTest(ITestOutputHelper output)
            : base(output)
        { }

        static int WithNameofValue;
        static int WithoutNameofValue;

        class E1 : Event { }
        class E2 : Event { }

        class M_With_nameof : Machine
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_Init_on_entry_action))]
            [OnExit(nameof(psharp_Init_on_exit_action))]
            [OnEventGotoState(typeof(E1), typeof(Next), nameof(psharp_Init_E1_action))]
            class Init : MachineState
            {
            }

            [OnEntry(nameof(psharp_Next_on_entry_action))]
            [OnEventDoAction(typeof(E2), nameof(psharp_Next_E2_action))]
            class Next : MachineState
            {
            }

            protected void psharp_Init_on_entry_action()
            {
                WithNameofValue += 1;
                this.Raise(new E1());
            }

            protected void psharp_Init_on_exit_action()
            { WithNameofValue += 10; }

            protected void psharp_Next_on_entry_action()
            {
                WithNameofValue += 1000;
                this.Raise(new E2());
            }

            protected void psharp_Init_E1_action()
            { WithNameofValue += 100; }

            protected void psharp_Next_E2_action()
            { WithNameofValue += 10000; }
        }

        class M_Without_nameof : Machine
        {
            [Microsoft.PSharp.Start]
            [OnEntry("psharp_Init_on_entry_action")]
            [OnExit("psharp_Init_on_exit_action")]
            [OnEventGotoState(typeof(E1), typeof(Next), "psharp_Init_E1_action")]
            class Init : MachineState
            {
            }

            [OnEntry("psharp_Next_on_entry_action")]
            [OnEventDoAction(typeof(E2), "psharp_Next_E2_action")]
            class Next : MachineState
            {
            }

            protected void psharp_Init_on_entry_action()
            {
                WithoutNameofValue += 1;
                this.Raise(new E1());
            }

            protected void psharp_Init_on_exit_action()
            { WithoutNameofValue += 10; }

            protected void psharp_Next_on_entry_action()
            {
                WithoutNameofValue += 1000;
                this.Raise(new E2());
            }

            protected void psharp_Init_E1_action()
            { WithoutNameofValue += 100; }

            protected void psharp_Next_E2_action()
            { WithoutNameofValue += 10000; }
        }

        [Fact]
        public void TestAllNameofWithNameof()
        {
            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(M_With_nameof));
            });

            base.AssertSucceeded(test);
            Assert.Equal(11111, WithNameofValue);
        }

        [Fact]
        public void TestAllNameofWithoutNameof()
        {
            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(M_Without_nameof));
            });

            base.AssertSucceeded(test);
            Assert.Equal(11111, WithoutNameofValue);
        }
    }
}
