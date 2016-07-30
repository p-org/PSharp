//-----------------------------------------------------------------------
// <copyright file="GenericMachineTest.cs">
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
    public class GenericMachineTest
    {
        class Program<T> : Machine
        {
            T Item;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Item = default(T);
                this.Goto(typeof(Active));
            }

            [OnEntry(nameof(ActiveInit))]
            class Active : MachineState { }

            void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Program<int>));
            }
        }

        [TestMethod]
        public void TestGenericMachine()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            Assert.AreEqual(0, engine.TestReport.NumOfFoundBugs);
        }
    }
}
