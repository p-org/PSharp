using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class SendTests
    {
        [Fact(Timeout=5000)]
        public void TestSendStatement()
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
send(this.Target, e1);
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
            this.Send(this.Target,new e1());
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestSendStatementWithSinglePayload()
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
send(this.Target, e1, 10);
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
            this.Send(this.Target,new e1(10));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestSendStatementWithDoublePayload()
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
send(this.Target, e1, 10, s);
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
            this.Send(this.Target,new e1(10, s));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }
    }
}
