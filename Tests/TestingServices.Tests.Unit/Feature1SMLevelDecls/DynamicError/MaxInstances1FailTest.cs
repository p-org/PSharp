//-----------------------------------------------------------------------
// <copyright file="MaxInstances1FailTest.cs">
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
    [TestClass]
    public class MaxInstances1FailTest
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
            public E3() : base(-1, -1) { }
        }

        class E4 : Event { }

        class Unit : Event
        {
            public Unit() : base(1, -1) { }
        }

        class RealMachine : Machine
        {
            MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            class Init : MachineState { }

            void EntryInit()
            {
                GhostMachine = this.CreateMachine(typeof(GhostMachine));
                this.Send(GhostMachine, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Send(GhostMachine, new E1());
                this.Send(GhostMachine, new E1()); // error
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(Unit), typeof(S3))]
            class S2 : MachineState { }

            void EntryS2()
            {
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(E4), typeof(S3))]
            class S3 : MachineState { }

            void Action1()
            {
                this.Assert((this.ReceivedEvent as E2).Value == 100);
                this.Send(GhostMachine, new E3());
                this.Send(GhostMachine, new E3());
            }
        }

        class GhostMachine : Machine
        {
            MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(GhostInit))]
            class Init : MachineState { }

            void Configure()
            {
                RealMachine = (this.ReceivedEvent as Config).Id;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(E1), typeof(S1))]
            class GhostInit : MachineState { }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            [IgnoreEvents(typeof(E1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Send(RealMachine, new E2(100));
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(E3), typeof(GhostInit))]
            class S2 : MachineState { }

            void EntryS2()
            {
                this.Send(RealMachine, new E4());
                this.Send(RealMachine, new E4());
                this.Send(RealMachine, new E4());
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(RealMachine));
            }
        }

        [TestMethod]
        public void TestMaxInstances1AssertionFailure()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.MaxSchedulingSteps = 3;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            Assert.AreEqual(1, engine.NumOfFoundBugs);
        }
    }
}
