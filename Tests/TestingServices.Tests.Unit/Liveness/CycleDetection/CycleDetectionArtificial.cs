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
    public class CycleDetectionArtificial
    {
        class Message : Event { }

        class EventHandler : Machine
        {

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                this.Send(this.Id, new Message());
                this.Send(this.Id, new Message());
            }
        }

        class WatchDog : Monitor
        {
            public class NotifyMessage : Event { }

            [Start]
            [Hot]
            class HotState : MonitorState { }
        }

        [TestMethod]
        public void TestCycleDetectionArtificialBug()
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
                    runtime.CreateMachine(typeof(EventHandler));
                });
            engine.Run();

            Assert.AreEqual(0, engine.TestReport.NumOfFoundBugs);

            //string expected = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            //string actual = engine.TestReport.BugReports.First();
            //Assert.AreEqual(expected, actual);
        }
    }
}
