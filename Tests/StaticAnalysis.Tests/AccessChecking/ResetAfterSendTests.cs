using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class ResetAfterSendTests : BaseTest
    {
        public ResetAfterSendTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
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

            var configuration = Setup.GetConfiguration();
            configuration.ProjectName = "Test";
            configuration.ThrowInternalExceptions = true;
            configuration.IsVerbose = true;
            configuration.AnalyzeDataRaces = true;
            Assert.Succeeded(configuration, test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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
            Assert.Succeeded(test, isPSharpProgram: false);
        }
    }
}
