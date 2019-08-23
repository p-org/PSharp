// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class MethodTests
    {
        [Fact(Timeout=5000)]
        public void TestVoidMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Bar() { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }

        void Bar()
        { }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestVoidMethodDeclaration2()
        {
            var test = @"
namespace Foo {
machine M { start state S { } void Bar() { } }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }

        void Bar()
        { }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestPublicMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
public void Foo() { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine method cannot be public.", test);
        }

        [Fact(Timeout=5000)]
        public void TestInternalMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
internal void Foo() { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine method cannot be internal.", test);
        }

        [Fact(Timeout=5000)]
        public void TestMethodDeclarationWithoutBrackets()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Foo()
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\" or \";\".", test);
        }
    }
}
