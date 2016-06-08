//-----------------------------------------------------------------------
// <copyright file="SimpleTaskFailTest.cs">
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
    public class SimpleTaskFailTest
    {
        class Unit : Event { }

        class TaskCreator : Machine
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
                Task.Factory.StartNew(() =>
                {
                    this.Value++;
                });

                this.Assert(this.Value == 0, "Value is '{0}' (expected '0').", this.Value);
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
        public void TestSimpleTaskFail()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 2;
            configuration.ScheduleIntraMachineConcurrency = true;

            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration, TestProgram.Execute).Run();
            var bugReport = "Value is '1' (expected '0').";
            Assert.AreEqual(bugReport, engine.BugReport);
        }
    }
}
