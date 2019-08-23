// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class NoStatementsTest : BaseTest
    {
        public NoStatementsTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestNoStatements()
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

 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }
    }
}
