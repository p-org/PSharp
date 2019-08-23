using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class UsingStatementTests : BaseTest
    {
        public UsingStatementTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestUsingStatementFail()
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

  using (System.IO.BinaryReader br = new System.IO.BinaryReader(
    new System.IO.MemoryStream()))
  {
   this.Send(this.Target, new eUnit(letter));
   letter.Text = ""text"";
  }
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }
    }
}
