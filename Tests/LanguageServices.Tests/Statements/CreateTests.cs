// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class CreateTests
    {
        [Fact(Timeout=5000)]
        public void TestCreateStatement()
        {
            var test = @"
namespace Foo {
public event e1;

machine M {
machine Target;
start state S
{
entry
{
create(M);
}
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
        MachineId Target;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {
            this.CreateMachine(typeof(M));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestCreateNamedMachineStatement()
        {
            var test = @"
namespace Foo {
public event e1;

machine M {
machine Target;
start state S
{
entry
{
create(M, ""NamedMachine"");
}
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
        MachineId Target;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {
            this.CreateMachine(typeof(M),""NamedMachine"");
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestCreateStatementWithSinglePayload()
        {
            var test = @"
namespace Foo {
public event e1 (k:int);

machine M {
machine Target;
start state S
{
entry
{
create(M, e1, 10);
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1 : Event
    {
        public int k;

        public e1(int k)
            : base()
        {
            this.k = k;
        }
    }

    class M : Machine
    {
        MachineId Target;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {
            this.CreateMachine(typeof(M),new e1(10));
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestCreateStatementWithDoublePayload()
        {
            var test = @"
namespace Foo {
public event e1 (k:int, s:string);

machine M {
machine Target;
start state S
{
entry
{
string s = ""hello"";
create(M, e1, 10, s);
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1 : Event
    {
        public int k;
        public string s;

        public e1(int k, string s)
            : base()
        {
            this.k = k;
            this.s = s;
        }
    }

    class M : Machine
    {
        MachineId Target;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {
            string s = ""hello"";
            this.CreateMachine(typeof(M),new e1(10, s));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestCreateNamedMachineStatementWithSinglePayload()
        {
            var test = @"
namespace Foo {
public event e1 (k:int);

machine M {
machine Target;
start state S
{
entry
{
create(M, ""NamedMachine"", e1, 10);
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1 : Event
    {
        public int k;

        public e1(int k)
            : base()
        {
            this.k = k;
        }
    }

    class M : Machine
    {
        MachineId Target;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {
            this.CreateMachine(typeof(M),""NamedMachine"",new e1(10));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestCreateNamedMachineStatementWithDoublePayload()
        {
            var test = @"
namespace Foo {
public event e1 (k:int, s:string);

machine M {
machine Target;
start state S
{
entry
{
string s = ""hello"";
create(M, ""NamedMachine"", e1, 10, s);
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1 : Event
    {
        public int k;
        public string s;

        public e1(int k, string s)
            : base()
        {
            this.k = k;
            this.s = s;
        }
    }

    class M : Machine
    {
        MachineId Target;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {
            string s = ""hello"";
            this.CreateMachine(typeof(M),""NamedMachine"",new e1(10, s));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }
    }
}
