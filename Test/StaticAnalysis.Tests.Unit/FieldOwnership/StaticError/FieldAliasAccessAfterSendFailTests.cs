//-----------------------------------------------------------------------
// <copyright file="FieldAliasAccessAfterSendFailTests.cs">
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
    public class FieldAliasAccessAfterSendFailTests : BasePSharpTest
    {
        [TestMethod, Timeout(10000)]
        public void TestFieldAliasAccessAfterSend1Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Letter Letter;
 
 public eUnit(Letter letter)
  : base()
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  var otherLetter = this.Letter;
  this.Send(this.Target, new eUnit(this.Letter));
  otherLetter.Text = ""changed"";
  otherLetter.Num = 1;
 }
}
}";

            var solution = base.GetSolution(test);

            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.Verbose = 2;
            configuration.DoStateTransitionAnalysis = false;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '3' errors";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldAliasAccessAfterSend2Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Letter Letter;
 
 public eUnit(Letter letter)
  : base()
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  this.Letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(this.Letter));
  this.Foo();
 }

 void Foo()
 {
  this.Bar();
 }

 void Bar()
 {
  this.Letter.Text = ""text2""; // ERROR
 }
}
}";

            var solution = base.GetSolution(test);

            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.Verbose = 2;
            configuration.DoStateTransitionAnalysis = false;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '2' errors";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }
    }
}
