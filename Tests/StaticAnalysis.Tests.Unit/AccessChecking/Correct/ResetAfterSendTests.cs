//-----------------------------------------------------------------------
// <copyright file="ResetAfterSendTests.cs">
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
    public class ResetAfterSendTests : BaseTest
    {
        [TestMethod, Timeout(10000)]
        public void TestResetGivenUpReferenceAfterSend1()
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = new Letter(""Bangalore"");
  var text = letter.Text;
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.ProjectName = "Test";
            configuration.ThrowInternalExceptions = true;
            configuration.Verbose = 2;
            configuration.AnalyzeDataRaces = true;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestResetGivenUpReferenceAfterSend2()
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  letter = new Letter(""test2"");
  letter.Text = ""changed"";
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
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestResetViaFieldAfterSend1()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  this.Letter = letter;
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
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestResetViaFieldAfterSend2()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  this.Letter.Text = ""Bangalore"";
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
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestResetViaFieldAfterSend3()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  letter.Text = ""Bangalore"";
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
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestResetViaFieldAfterSend4()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  var text = this.Letter.Text;
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
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }

        [TestMethod, Timeout(10000)]
        public void TestResetViaFieldAfterSend5()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  var text = letter.Text;
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
            var expected = "... No static analysis errors detected";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }
    }
}
