//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine17Test.cs">
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

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class SEMOneMachine17Test
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
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E1());
            }

            void ExitInit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(EntryS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Raise(new E1());
            }

            void Action1() { }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Real1));
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "push" with implicit "pop" when the
        /// unhandled event was raised. This test checks implicit popping of the
        /// state when there's an unhandled event which was raised.
        /// </summary>
        [TestMethod]
        public void TestPushImplicitPopWithRaise()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(1, engine.NumOfFoundBugs);
        }
    }
}
