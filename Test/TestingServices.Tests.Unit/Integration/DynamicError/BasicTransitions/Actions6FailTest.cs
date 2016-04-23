//-----------------------------------------------------------------------
// <copyright file="Actions6FailTest.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    /// <summary>
    /// Tests that event payload works correctly with a push transition.
    /// </summary>
    [TestClass]
    public class Actions6FailTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public int Value;
            public E2(int value) : base(1, -1) { this.Value = value; }
        }

        class E3 : Event
        {
            public E3() : base(1, -1) { }
        }

        class E4 : Event
        {
            public E4() : base(1, -1) { }
        }

        class Unit : Event
        {
            public Unit() : base(1, -1) { }
        }

        class Real : Machine
        {
            MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            class Init : MachineState { }

            void EntryInit()
            {
                GhostMachine = this.CreateMachine(typeof(Ghost));
                this.Send(GhostMachine, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Send(GhostMachine, new E1());
            }

            [OnEntry(nameof(EntryS2))]
            class S2 : MachineState { }

            void EntryS2()
            {
                // this assert is reachable
                this.Assert(false);
            }

            void Action1()
            {
                this.Assert((this.ReceivedEvent as E2).Value == 100); // this assert passes
                this.Send(GhostMachine, new E3());
            }
        }

        class Ghost : Machine
        {
            MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            class Init : MachineState { }

            void Configure()
            {
                RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Send(RealMachine, new E2(100));
            }

            [OnEntry(nameof(EntryS2))]
            class S2 : MachineState { }

            void EntryS2()
            {
                this.Send(RealMachine, new E4());
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Real));
            }
        }

        [TestMethod]
        public void TestActions6Fail()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(1, engine.NumOfFoundBugs);
            Assert.AreEqual(10, engine.ExploredDepth);
        }
    }
}
