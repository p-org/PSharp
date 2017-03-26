//-----------------------------------------------------------------------
// <copyright file="CycleDetectionCounterTest.cs">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class CycleDetectionCounterTest
    {
        class Configure: Event
        {
            public bool CacheCounter;
            public bool ResetCounter;

            public Configure(bool cacheCounter, bool resetCounter)
            {
                this.CacheCounter = cacheCounter;
                this.ResetCounter = resetCounter;
            }
        }

        class Message : Event { }

        class EventHandler : Machine
        {
            int Counter;
            bool CacheCounter;
            bool ResetCounter;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.Counter = 0;
                this.CacheCounter = (this.ReceivedEvent as Configure).CacheCounter;
                this.ResetCounter = (this.ReceivedEvent as Configure).ResetCounter;
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                this.Send(this.Id, new Message());
                this.Counter++;
                if (this.ResetCounter && this.Counter == 10)
                {
                    this.Counter = 0;
                }
            }

            protected override int HashedState
            {
                get
                {
                    if (this.CacheCounter)
                    {
                        // The counter contributes to the cached machine state.
                        // This allows the liveness checker to detect progress.
                        return this.Counter;
                    }
                    else
                    {
                        return base.HashedState;
                    }
                }
            }
        }

        class WatchDog : Monitor
        {
            [Start]
            [Hot]
            class HotState : MonitorState { }
        }

        [TestMethod]
        public void TestCycleDetectionCounterNoBug()
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
                    runtime.CreateMachine(typeof(EventHandler), new Configure(true, false));
                });
            engine.Run();

            Assert.AreEqual(0, engine.TestReport.NumOfFoundBugs);
        }

        [TestMethod]
        public void TestCycleDetectionCounterBug()
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
                    runtime.CreateMachine(typeof(EventHandler), new Configure(false, false));
                });
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);

            string expected = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            string actual = engine.TestReport.BugReports.First();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestCycleDetectionCounterResetBug()
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
                    runtime.CreateMachine(typeof(EventHandler), new Configure(true, true));
                });
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);

            string expected = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            string actual = engine.TestReport.BugReports.First();
            Assert.AreEqual(expected, actual);
        }
    }
}
