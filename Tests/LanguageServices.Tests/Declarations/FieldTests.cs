using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class FieldTests
    {
        [Fact(Timeout=5000)]
        public void TestIntFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
int k;
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        int k;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestListFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
List<int> k;
start state S { }
}
}";

            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        List<int> k;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestMachineFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        MachineId N;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestMachineArrayFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine[] MachineArray;
List<machine> MachineList; 
List<machine[]> MachineArrayList; 
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        MachineId[] MachineArray;
        List<MachineId> MachineList;
        List<MachineId[]> MachineArrayList;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestPublicFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
public int k;
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine field cannot be public.", test);
        }

        [Fact(Timeout=5000)]
        public void TestInternalFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
internal int k;
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine field cannot be internal.", test);
        }

        [Fact(Timeout=5000)]
        public void TestIntFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
int k
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
machine N
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestPrivateMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
private machine N
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }
    }
}
