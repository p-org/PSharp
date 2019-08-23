// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class NoStatementsWithLoopTest : BaseTest
    {
        public NoStatementsWithLoopTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestNoStatementsWithLoop()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  for (int i = 0; i < 2; i++) { }
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }
    }
}
