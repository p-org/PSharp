//-----------------------------------------------------------------------
// <copyright file="IfStatementTests.cs">
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
    public class IfStatementTests
    {
        [TestMethod, Timeout(10000)]
        public void TestIfStatement1()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = letter;

  if (num == 0)
  {
   otherLetter = new Letter(""Bangalore"");
  }
  else
  {
   otherLetter = new Letter(""Redmond"");
  }

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;
            configuration.EnableDataRaceAnalysis = true;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... No static analysis errors detected (but absolutely no warranty provided)";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestIfStatement2()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = letter;

  if (num == 0)
  {
   otherLetter = letter;
  }
  else
  {
   otherLetter = new Letter(""Redmond"");
  }

  otherLetter = new Letter(""Bangalore"");

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;
            configuration.EnableDataRaceAnalysis = true;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

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
