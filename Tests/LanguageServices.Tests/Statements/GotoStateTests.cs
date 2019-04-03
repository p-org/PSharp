// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class GotoStateTests
    {
        [Fact]
        public void TestGotoStateStatement()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
entry
{
jump(S2);
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
            this.Goto<S2>();
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestGotoStateStatementWithPSharpAPI()
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
this.Goto<S2>();
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
this.Goto<S2>();
}
}
}";
            // Note: original formatting is preserved for CSharp rewriting.
            LanguageTestUtilities.AssertRewritten(expected, test, isPSharpProgram: false);
        }

        [Fact]
        public void TestGotoStateStatementWithPSharpAPI2()
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
            this.Goto<S2>();
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
            this.Goto<S2>();
        }
    }
}";
            // Note: original formatting is preserved for CSharp rewriting.
            LanguageTestUtilities.AssertRewritten(expected, test, isPSharpProgram: false);
        }
    }
}
