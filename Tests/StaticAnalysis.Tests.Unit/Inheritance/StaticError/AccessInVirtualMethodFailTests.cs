//-----------------------------------------------------------------------
// <copyright file="AccessInVirtualMethodFailTests.cs">
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
    public class AccessInVirtualMethodFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestAccessInVirtualMethod1Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = new SuperEnvelope();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod2Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope;
  if (letter.Text == """")
  {
    envelope = new Envelope();
  }
  else
  {
    envelope = new SuperEnvelope();
  }

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod3Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope;
  if (letter.Text == """")
    envelope = new Envelope();
  else
    envelope = new SuperEnvelope();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod4Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return new SuperEnvelope();
  }
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod5Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return this.Bar();
  }
 }

 Envelope Bar()
 {
  return new SuperEnvelope();
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod6Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return this.Bar();
  }
 }

 Envelope Bar()
 {
  Envelope envelope = new SuperEnvelope();
  return envelope;
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod7Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }

 internal virtual void Bar(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }

 internal override void Bar(Letter letter)
 {
  base.Letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  Envelope envelope = new SuperEnvelope();
  Envelope anotherEnvelope = new Envelope();
  anotherEnvelope = envelope;

  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return anotherEnvelope;
  }
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod8Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore""; // ERROR
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");
  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  this.Bar(envelope, letter);
 }

 Envelope Foo()
 {
  Envelope someEnvelope = new SuperEnvelope();
  Envelope anotherEnvelope;
  anotherEnvelope = new Envelope();
  anotherEnvelope = someEnvelope;

  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return anotherEnvelope;
  }
 }

 void Bar(Envelope envelope, Letter letter)
 {
  this.FooBar(envelope, letter);
 }

 void FooBar(Envelope envelope, Letter letter)
 {
  envelope.Foo(letter);
 }
}
}";

            var configuration = Configuration.Create();
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
        public void TestAccessInVirtualMethod9Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  Letter = letter; // ERROR
  base.Letter.Text = ""Bangalore""; // ERROR
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");
  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  this.Bar(envelope, letter);
 }

 Envelope Foo()
 {
  Envelope someEnvelope = new SuperEnvelope();
  Envelope anotherEnvelope;
  anotherEnvelope = new Envelope();
  anotherEnvelope = someEnvelope;

  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return anotherEnvelope;
  }
 }

 void Bar(Envelope envelope, Letter letter)
 {
  this.FooBar(envelope, letter);
 }

 void FooBar(Envelope envelope, Letter letter)
 {
  envelope.Foo(letter);
 }
}
}";

            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.ThrowInternalExceptions = true;
            configuration.Verbose = 2;
            configuration.AnalyzeDataRaces = true;
            configuration.DoStateTransitionAnalysis = false;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '3' errors";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }
    }
}
