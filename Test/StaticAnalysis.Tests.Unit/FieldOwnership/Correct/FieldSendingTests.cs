//-----------------------------------------------------------------------
// <copyright file="FieldSendingTests.cs">
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

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    [TestClass]
    public class FieldSendingTests : BasePSharpTest
    {
        [TestMethod, Timeout(3000)]
        public void TestFieldSending()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public MachineId Id;
 
 public eUnit(MachineId id)
  : base()
 {
  this.Id = id;
 }
}

class M : Machine
{
 MachineId Target;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(this.Id));
 }
}
}";

            var solution = base.GetSolution(test);

            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.Verbose = 2;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... No static analysis errors detected (but absolutely no warranty provided)";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }
    }
}
