using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AccessAfterCreateMachineTests : BaseTest
    {
        public AccessAfterCreateMachineTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestAccessAfterCreateMachine()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public int Value;
 
 public eUnit(int value)
  : base()
 {
  this.Value = value;
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
  int value = 0;
  this.Target = this.CreateMachine(typeof(M), new eUnit(value));
  value = 1;
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessAfterCreateMachineInCallee()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public int Value;
 
 public eUnit(int value)
  : base()
 {
  this.Value = value;
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
  int value = 0;
  this.Foo(value);
 }

 void Foo(int value)
 {
  this.Target = this.CreateMachine(typeof(M), new eUnit(value));
  value = 1;
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessAfterCreateMachineFail()
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
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
  letter.Text = ""changed"";
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessAfterCreateMachineInCalleeFail()
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
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
  letter.Text = ""changed"";
 }
}
}";
            var error = "Error: Method 'Foo' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }
    }
}
