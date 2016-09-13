//-----------------------------------------------------------------------
// <copyright file="GotoStateMultipleInActionFailTest.cs">
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
    public class GotoStateTopLevelActionFailTest
    {
        public enum ErrorType { CALL_GOTO, CALL_RAISE, CALL_SEND, ON_EXIT };
        public static ErrorType ErrorTypeVal = ErrorType.CALL_GOTO;

        class E : Event { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitMethod))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Foo();
                switch(ErrorTypeVal)
                {
                    case ErrorType.CALL_GOTO:
                        this.Goto(typeof(Done));
                        break;
                    case ErrorType.CALL_RAISE:
                        this.Raise(new E());
                        break;
                    case ErrorType.CALL_SEND:
                        this.Send(Id, new E());
                        break;
                    case ErrorType.ON_EXIT:
                        break;
                }
            }

            void ExitMethod()
            {
                this.Pop();
            }

            void Foo()
            {
                this.Goto(typeof(Done));
            }

            class Done : MachineState { }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute1(PSharpRuntime runtime)
            {
                ErrorTypeVal = ErrorType.CALL_GOTO;
                runtime.CreateMachine(typeof(Program));
            }
            [Test]
            public static void Execute2(PSharpRuntime runtime)
            {
                ErrorTypeVal = ErrorType.CALL_RAISE;
                runtime.CreateMachine(typeof(Program));
            }
            [Test]
            public static void Execute3(PSharpRuntime runtime)
            {
                ErrorTypeVal = ErrorType.CALL_SEND;
                runtime.CreateMachine(typeof(Program));
            }
            [Test]
            public static void Execute4(PSharpRuntime runtime)
            {
                ErrorTypeVal = ErrorType.ON_EXIT;
                runtime.CreateMachine(typeof(Program));
            }

        }

        [TestMethod]
        public void TestGotoStateTopLevelActionFail1()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute1);
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);
            Assert.AreEqual("Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.GotoStateTopLevelActionFailTest+Program(1)' " +
                "has called multiple raise/goto/pop in the same action.",
                engine.TestReport.BugReport);
        }

        [TestMethod]
        public void TestGotoStateTopLevelActionFail2()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute2);
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);
            Assert.AreEqual("Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.GotoStateTopLevelActionFailTest+Program(1)' " +
                "has called multiple raise/goto/pop in the same action.",
                engine.TestReport.BugReport);
        }

        [TestMethod]
        public void TestGotoStateTopLevelActionFail3()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute3);
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);
            Assert.AreEqual("Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.GotoStateTopLevelActionFailTest+Program(1)' " +
                "cannot call API 'Send' after calling raise/goto/pop in the same action.",
                engine.TestReport.BugReport);
        }

        [TestMethod]
        public void TestGotoStateTopLevelActionFail4()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute4);
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);
            Assert.AreEqual("Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.GotoStateTopLevelActionFailTest+Program(1)' " +
                "has called raise/goto/pop inside an OnExit method.",
                engine.TestReport.BugReport);
        }

    }
}