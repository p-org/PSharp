// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class MemberVisibilityTests : BaseTest
    {
        public MemberVisibilityTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestMemberVisibility()
        {
            var test = @"
namespace Foo {
class M : Machine
{
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestPublicFieldVisibility()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 public int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            var error = "Warning: Field 'int Num' of machine 'Foo.M' is declared as " +
                "'public'.   at 'public int Num;' in Program.cs:line 7";
            Assert.Warning(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestPublicMethodVisibility()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 public void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            var error = "Warning: Method 'FirstOnEntryAction' of machine 'Foo.M' is " +
                "declared as 'public'.   at 'FirstOnEntryAction' in Program.cs:line 13";
            Assert.Warning(test, 1, error, isPSharpProgram: false);
        }
    }
}
