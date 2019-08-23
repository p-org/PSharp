using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class PushStateTests
    {
        [Fact(Timeout=5000)]
        public void TestPushStateStatement()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
entry
{
push(S2);
}
}
state S2
{
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Push<S2>();
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestPushStateStatementWithPSharpAPI()
        {
            var test = @"
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Push<S2>();
}
}
}";
            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Push<S2>();
}
}
}";
            // Note: original formatting is preserved for CSharp rewriting.
            LanguageTestUtilities.AssertRewritten(expected, test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestPushStateStatementWithPSharpAPI2()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Push<S2>();
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Push<S2>();
        }
    }
}";
            // Note: original formatting is preserved for CSharp rewriting.
            LanguageTestUtilities.AssertRewritten(expected, test, isPSharpProgram: false);
        }
    }
}
