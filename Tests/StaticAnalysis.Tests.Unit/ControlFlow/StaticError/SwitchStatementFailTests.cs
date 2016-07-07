//-----------------------------------------------------------------------
// <copyright file="SwitchStatementFailTests.cs">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    [TestClass]
    public class SwitchStatementFailTests : BaseTest
    {
        [TestMethod, Timeout(10000)]
        public void TestSwitchStatement1Fail()
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
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");

  this.Send(this.Target, new eUnit(letter));

  switch (this.Num)
  {
   case 0:
   case 1:
    letter = new Letter(""Taipei"");
    break;

   case 2:
    letter.Text = ""text"";
    break;

   default:
    break;
  }
 }
}
}";

            var configuration = base.GetConfiguration();

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '1' error";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            var actual = IO.GetOutput();

            Assert.AreEqual(error.Replace(Environment.NewLine, string.Empty),
               actual.Substring(0, actual.IndexOf(Environment.NewLine)));

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestSwitchStatement2Fail()
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
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");

  this.Send(this.Target, new eUnit(letter));

  switch (this.Num)
  {
   case 0:
   case 1:
    letter = new Letter(""Taipei"");
    break;

   case 2:
    letter = new Letter(""Bangalore"");
    break;

   default:
    letter.Text = ""text"";
    break;
  }
 }
}
}";

            var configuration = base.GetConfiguration();

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '1' error";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            var actual = IO.GetOutput();

            Assert.AreEqual(error.Replace(Environment.NewLine, string.Empty),
               actual.Substring(0, actual.IndexOf(Environment.NewLine)));

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestSwitchStatement3Fail()
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
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");

  this.Send(this.Target, new eUnit(letter));

  switch (this.Num)
  {
   case 0:
   case 1:
    letter = new Letter(""Taipei"");
    break;

   case 2:
    letter = new Letter(""Bangalore"");
    break;

   default:
    break;
  }

  letter.Text = ""text"";
 }
}
}";

            var configuration = base.GetConfiguration();

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '1' error";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            var actual = IO.GetOutput();

            Assert.AreEqual(error.Replace(Environment.NewLine, string.Empty),
               actual.Substring(0, actual.IndexOf(Environment.NewLine)));

            IO.StopWritingToMemory();
        }
    }
}
