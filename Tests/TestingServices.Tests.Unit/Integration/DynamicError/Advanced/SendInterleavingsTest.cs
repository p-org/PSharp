//-----------------------------------------------------------------------
// <copyright file="SendInterleavingsTest.cs">
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
    public class SendInterleavingsTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class Event1 : Event { }
        class Event2 : Event { }

        class Receiver : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Event1), nameof(OnEvent1))]
            [OnEventDoAction(typeof(Event2), nameof(OnEvent2))]
            class Init : MachineState { }

            int count1 = 0;
            void Initialize()
            {
                var s1 = CreateMachine(typeof(Sender1));
                this.Send(s1, new Config(this.Id));
                var s2 = CreateMachine(typeof(Sender2));
                this.Send(s2, new Config(this.Id));
            }

            void OnEvent1()
            {
                count1++;
            }
            void OnEvent2()
            {
                Assert(count1 != 1);
            }
        }

        class Sender1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            class State : MachineState { }

            void Run()
            {
                Send((this.ReceivedEvent as Config).Id, new Event1());
                Send((this.ReceivedEvent as Config).Id, new Event1());
            }
        }

        class Sender2 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            class State : MachineState { }

            void Run()
            {
                Send((this.ReceivedEvent as Config).Id, new Event2());
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Receiver));
            }
        }

        [TestMethod]
        public void TestSendInterleavingsAssertionFailure()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 49;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);
        }
    }
}
