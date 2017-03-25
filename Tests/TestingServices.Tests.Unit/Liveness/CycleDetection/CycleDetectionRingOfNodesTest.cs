//-----------------------------------------------------------------------
// <copyright file="CycleDetectionRingOfNodesTest.cs">
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

using System.Linq;

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class CycleDetectionRingOfNodesTest
    {
        class Configure: Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        class Message : Event { }

        class Environment : Machine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                var applyFix = (this.ReceivedEvent as Configure).ApplyFix;
                var machine1 = this.CreateMachine(typeof(Node), new Configure(applyFix));
                var machine2 = this.CreateMachine(typeof(Node), new Configure(applyFix));
                this.Send(machine1, new Node.SetNeighbour(machine2));
                this.Send(machine2, new Node.SetNeighbour(machine1));
            }
        }

        class Node : Machine
        {
            public class SetNeighbour : Event
            {
                public MachineId Next;

                public SetNeighbour(MachineId next)
                {
                    this.Next = next;
                }
            }

            MachineId Next;
            bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(SetNeighbour), nameof(OnSetNeighbour))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
            }

            void OnSetNeighbour()
            {
                var e = ReceivedEvent as SetNeighbour;
                this.Next = e.Next;
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                if (Next != null)
                {
                    this.Send(Next, new Message());
                    if (this.ApplyFix)
                    {
                        this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                    }
                }
            }
        }

        class WatchDog : Monitor
        {
            public class NotifyMessage : Event { }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(ColdState))]
            class HotState : MonitorState { }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            class ColdState : MonitorState { }
        }

        [TestMethod]
        public void TestCycleDetectionRingOfNodesNoBug()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.CacheProgramState = true;
            configuration.EnableCycleReplayingStrategy = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration,
                (runtime) => {
                    runtime.RegisterMonitor(typeof(WatchDog));
                    runtime.CreateMachine(typeof(Environment), new Configure(true));
                });
            engine.Run();

            Assert.AreEqual(0, engine.TestReport.NumOfFoundBugs);
        }

        [TestMethod]
        public void TestCycleDetectionRingOfNodesBug()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.CacheProgramState = true;
            configuration.EnableCycleReplayingStrategy = true;
            configuration.MaxSchedulingSteps = 200;

            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration,
                (runtime) => {
                    runtime.RegisterMonitor(typeof(WatchDog));
                    runtime.CreateMachine(typeof(Environment), new Configure(false));
                });
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);

            string expected = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            string actual = engine.TestReport.BugReports.First();
            Assert.AreEqual(expected, actual);
        }
    }
}
