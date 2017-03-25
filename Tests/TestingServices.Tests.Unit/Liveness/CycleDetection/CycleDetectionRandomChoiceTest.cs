//-----------------------------------------------------------------------
// <copyright file="CycleDetectionRandomChoiceTest.cs">
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
    public class CycleDetectionRandomChoiceTest
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

        class EventHandler : Machine
        {
            bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                this.Send(this.Id, new Message());
                this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                if (this.Choose())
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyDone());
                    this.Raise(new Halt());
                }
            }

            bool Choose()
            {
                if (this.ApplyFix)
                {
                    return this.FairRandom();
                }
                else
                {
                    return this.Random();
                }
            }
        }

        class WatchDog : Monitor
        {
            public class NotifyMessage : Event { }
            public class NotifyDone : Event { }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            [OnEventGotoState(typeof(NotifyDone), typeof(ColdState))]
            class HotState : MonitorState { }

            [Cold]
            class ColdState : MonitorState { }
        }

        [TestMethod]
        public void TestCycleDetectionRandomChoiceNoBug()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.CacheProgramState = true;
            configuration.EnableCycleReplayingStrategy = true;
            configuration.RandomSchedulingSeed = 906;
            configuration.SchedulingIterations = 7;
            configuration.MaxSchedulingSteps = 200;

            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration,
                (runtime) => {
                    runtime.RegisterMonitor(typeof(WatchDog));
                    runtime.CreateMachine(typeof(EventHandler), new Configure(true));
                });
            engine.Run();

            Assert.AreEqual(0, engine.TestReport.NumOfFoundBugs);
        }

        [TestMethod]
        public void TestCycleDetectionRandomChoiceBug()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.CacheProgramState = true;
            configuration.EnableCycleReplayingStrategy = true;
            configuration.RandomSchedulingSeed = 906;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration,
                (runtime) => {
                    runtime.RegisterMonitor(typeof(WatchDog));
                    runtime.CreateMachine(typeof(EventHandler), new Configure(false));
                });
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);

            string expected = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            string actual = engine.TestReport.BugReports.First();
            Assert.AreEqual(expected, actual);
        }
    }
}
