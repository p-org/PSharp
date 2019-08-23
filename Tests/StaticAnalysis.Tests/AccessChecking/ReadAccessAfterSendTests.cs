using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class ReadAccessAfterSendTests : BaseTest
    {
        public ReadAccessAfterSendTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestReadAccessAfterSend1Fail()
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
  var text = letter.Text;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestReadAccessAfterSend2Fail()
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
  var text = letter.Text;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestReadAccessAfterSendStoreInFieldFail()
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
 string Text;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  this.Text = letter.Text;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestReadAccessAfterSendInCalleeFail()
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
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Send(this.Target, new eUnit(letter));
  var text = letter.Text;
 }
}
}";
            var error = "Error: Method 'Foo' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }
    }
}
