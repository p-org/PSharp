// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AccessAfterSendTests : BaseTest
    {
        public AccessAfterSendTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestAccessAfterSend()
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
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(value));
  value = 1;
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessAfterSendInCallee()
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
  this.Target = this.CreateMachine(typeof(M));
  this.Foo(value);
 }

 void Foo(int value)
 {
  this.Send(this.Target, new eUnit(value));
  value = 1;
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }
    }
}
