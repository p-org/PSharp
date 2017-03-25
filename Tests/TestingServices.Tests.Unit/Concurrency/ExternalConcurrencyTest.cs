//-----------------------------------------------------------------------
// <copyright file="ExternalConcurrencyTest.cs">
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class ExternalConcurrencyTest
    {
        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                Task task = Task.Factory.StartNew(() => {
                    this.Send(this.Id, new E());
                });
                task.Wait();
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                Task task = Task.Factory.StartNew(() => {
                    this.Random();
                });
                task.Wait();
            }
        }

        [TestMethod]
        public void TestExternalTaskSendingEvent()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, (r) => { r.CreateMachine(typeof(M)); });
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);

            string expected = @"Detected task with id '' that is not controlled by the P# runtime.";
            string actual = Regex.Replace(engine.TestReport.BugReports.First(), "[0-9]", "");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestExternalTaskInvokingRandom()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, (r) => { r.CreateMachine(typeof(N)); });
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);

            string expected = @"Detected task with id '' that is not controlled by the P# runtime.";
            string actual = Regex.Replace(engine.TestReport.BugReports.First(), "[0-9]", "");
            Assert.AreEqual(expected, actual);
        }
    }
}
