using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AccessBeforeCreateMachineTests : BaseTest
    {
        public AccessBeforeCreateMachineTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestAccessBeforeCreateMachine()
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
  letter.Text = ""changed"";
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessBeforeCreateMachineInCallee()
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
  letter.Text = ""changed"";
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }
    }
}
