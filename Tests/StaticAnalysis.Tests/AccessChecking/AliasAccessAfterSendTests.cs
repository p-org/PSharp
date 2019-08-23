using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AliasAccessAfterSendTests : BaseTest
    {
        public AliasAccessAfterSendTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestAliasAccessAfterSendFail()
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  var otherLetter = letter;
  this.Send(this.Target, new eUnit(letter));
  otherLetter.Text = ""changed"";
  otherLetter.Num = 1;
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }
    }
}
