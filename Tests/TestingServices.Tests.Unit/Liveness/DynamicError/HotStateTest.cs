//-----------------------------------------------------------------------
// <copyright file="HotStateTest.cs">
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
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class HotStateTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class MConfig : Event
        {
            public List<MachineId> Ids;
            public MConfig(List<MachineId> ids) : base(-1, -1) { this.Ids = ids; }
        }

        class Unit : Event { }
        class DoProcessing : Event { }
        class FinishedProcessing : Event { }
        class NotifyWorkerIsDone : Event { }

        class Master : Machine
        {
            List<MachineId> Workers;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Workers = new List<MachineId>();

                for (int idx = 0; idx < 3; idx++)
                {
                    var worker = this.CreateMachine(typeof(Worker));
                    this.Send(worker, new Config(this.Id));
                    this.Workers.Add(worker);
                }

                this.Monitor<M>(new MConfig(this.Workers));

                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(FinishedProcessing), nameof(ProcessWorkerIsDone))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                foreach (var worker in this.Workers)
                {
                    this.Send(worker, new DoProcessing());
                }
            }

            void ProcessWorkerIsDone()
            {
                this.Monitor<M>(new NotifyWorkerIsDone());
            }
        }

        class Worker : Machine
        {
            MachineId Master;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Processing))]
            class Init : MachineState { }

            void Configure()
            {
                this.Master = (this.ReceivedEvent as Config).Id;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(DoProcessing), typeof(Done))]
            class Processing : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            void DoneOnEntry()
            {
                if (this.Random())
                {
                    this.Send(this.Master, new FinishedProcessing());
                }

                this.Raise(new Halt());
            }
        }

        class M : Monitor
        {
            List<MachineId> Workers;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(MConfig), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Done))]
            [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
            class Init : MonitorState { }

            void Configure()
            {
                this.Workers = (this.ReceivedEvent as MConfig).Ids;
            }

            void ProcessNotification()
            {
                this.Workers.RemoveAt(0);

                if (this.Workers.Count == 0)
                {
                    this.Raise(new Unit());
                }
            }

            class Done : MonitorState { }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.RegisterMonitor(typeof(M));
                runtime.CreateMachine(typeof(Master));
            }
        }

        [TestMethod]
        public void TestHotStateMonitor()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            IO.Debugging = true;

            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration, TestProgram.Execute).Run();
            var bugReport = "Monitor 'M' detected liveness property violation in hot state 'Init'.";
            Assert.AreEqual(bugReport, engine.BugReport);
        }
    }
}
