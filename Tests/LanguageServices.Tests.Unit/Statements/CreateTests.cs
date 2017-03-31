//-----------------------------------------------------------------------
// <copyright file="CreateTests.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class CreateTests
    {
        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
