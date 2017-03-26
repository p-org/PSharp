//-----------------------------------------------------------------------
// <copyright file="GroupStateFailTest.cs">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class GroupStateFailTest
    {
        class E : Event { }

        class Program : Machine
        {
            class States1 : StateGroup
            {
                [Start]
                [OnEntry(nameof(States1S1OnEntry))]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MachineState { }

                [OnEntry(nameof(States1S2OnEntry))]
                [OnEventGotoState(typeof(E), typeof(States2.S1))]
                public class S2 : MachineState { }
            }

            class States2 : StateGroup
            {
                [OnEntry(nameof(States2S1OnEntry))]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MachineState { }

                [OnEntry(nameof(States2S2OnEntry))]
                public class S2 : MachineState { }
            }

            void States1S1OnEntry()
            {
                this.Raise(new E());
            }

            void States1S2OnEntry()
            {
                this.Raise(new E());
            }

            void States2S1OnEntry()
            {
                this.Raise(new E());
            }

            void States2S2OnEntry()
            {
                this.Monitor<M>(new E());
            }
        }

        class M : Monitor
        {
            class States1 : StateGroup
            {
                [Start]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MonitorState { }

                [OnEntry(nameof(States1S2OnEntry))]
                [OnEventGotoState(typeof(E), typeof(States2.S1))]
                public class S2 : MonitorState { }
            }

            class States2 : StateGroup
            {
                [OnEntry(nameof(States2S1OnEntry))]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MonitorState { }

                [OnEntry(nameof(States2S2OnEntry))]
                public class S2 : MonitorState { }
            }

            void States1S2OnEntry()
            {
                this.Raise(new E());
            }

            void States2S1OnEntry()
            {
                this.Raise(new E());
            }

            void States2S2OnEntry()
            {
                this.Assert(false);
            }
        }
        
        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.RegisterMonitor(typeof(M));
                runtime.CreateMachine(typeof(Program));
            }
        }

        [TestMethod]
        public void TestGroupState()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            var bugReport = "Assertion failure.";
            Assert.IsTrue(engine.TestReport.BugReports.Count == 1);
            Assert.IsTrue(engine.TestReport.BugReports.Contains(bugReport));
        }
    }
}
