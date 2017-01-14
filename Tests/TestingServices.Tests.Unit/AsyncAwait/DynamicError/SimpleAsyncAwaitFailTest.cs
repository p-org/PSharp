//-----------------------------------------------------------------------
// <copyright file="SimpleAsyncAwaitFailTest.cs">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class SimpleAsyncAwaitFailTest
    {
        class Unit : Event { }

        internal class TaskCreator : Machine
        {
            int Value;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Value = 0;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                Process();
                this.Assert(this.Value < 3, "Value is '{0}' (expected less than '3').", this.Value);
            }

            async void Process()
            {
                Task t = Increment();
                this.Value++;
                await t;
                this.Value++;
            }

            Task Increment()
            {
                Task t = new Task(() => {
                    this.Value++;
                });

                t.Start();
                return t;
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(TaskCreator));
            }
        }

        [TestMethod]
        public void TestSimpleAsyncAwaitFail()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingIterations = 10;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            var bugReport = "Value is '3' (expected less than '3').";
            Assert.AreEqual(bugReport, engine.TestReport.BugReport);
        }
    }
}
