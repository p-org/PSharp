// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class RaiseTests
    {
        [Fact]
        public void TestEventRaiseStatement()
        {
            var test = @"
namespace Foo {
public event e1;

machine M {
start state S1
{
entry
{
raise(e1);
}
on e1 goto S2;
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
    public class e1 : Event
    {
        public e1()
            : base()
        {
        }
    }

    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        [OnEventGotoState(typeof(e1), typeof(S2))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Raise(new e1());
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestEventRaiseStatementWithPSharpAPI()
        {
            var test = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public e1()
  : base()
 { }
}

class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
[OnEventGotoState(typeof(e1), typeof(S2))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Raise(new e1());
}
}
}";
            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public e1()
  : base()
 { }
}

class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
[OnEventGotoState(typeof(e1), typeof(S2))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Raise(new e1());
}
}
}";
            LanguageTestUtilities.AssertRewritten(expected, test, isPSharpProgram: false);
        }
    }
}
