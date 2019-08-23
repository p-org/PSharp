using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class FieldAliasAccessAfterSendTests : BaseTest
    {
        public FieldAliasAccessAfterSendTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
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

            var configuration = Setup.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            Assert.Failed(configuration, test, 3, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
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

            var configuration = Setup.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            Assert.Failed(configuration, test, 2, isPSharpProgram: false);
        }
    }
}
