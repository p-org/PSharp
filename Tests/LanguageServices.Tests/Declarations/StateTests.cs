using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class StateTests
    {
        [Fact(Timeout=5000)]
        public void TestStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 { }
state S2 { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestEntryDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry{}
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
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncEntryDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
async entry{ await Task.Delay(42); }
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
        [OnEntry(nameof(psharp_S_on_entry_action_async))]
        class S : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_S_on_entry_action_async()
        { await Task.Delay(42); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
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
        [OnExit(nameof(psharp_S_on_exit_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
async exit{ await Task.Delay(42); }
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
        [OnExit(nameof(psharp_S_on_exit_action_async))]
        class S : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_S_on_exit_action_async()
        { await Task.Delay(42); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
exit {}
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
        [OnEntry(nameof(psharp_S_on_entry_action))]
        [OnExit(nameof(psharp_S_on_exit_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {}

        protected void psharp_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
async entry { await Task.Delay(42); }
async exit { await Task.Delay(42); }
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
        [OnEntry(nameof(psharp_S_on_entry_action_async))]
        [OnExit(nameof(psharp_S_on_exit_action_async))]
        class S : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_S_on_entry_action_async()
        { await Task.Delay(42); }

        protected async System.Threading.Tasks.Task psharp_S_on_exit_action_async()
        { await Task.Delay(42); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2;
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
        [OnEventGotoState(typeof(e), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 goto S2;
on e2 goto S3;
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
        [OnEventGotoState(typeof(e1), typeof(S2))]
        [OnEventGotoState(typeof(e2), typeof(S3))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnSimpleGenericEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> goto S2;
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
        [OnEventGotoState(typeof(e<int>), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnComplexGenericEventGotoStateDeclaration()
        {
            var test = @"
using System.Collections.Generic;
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;
using System.Collections.Generic;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e<List<Tuple<bool, object>>, Dictionary<string, float>>), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnQualifiedEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on Foo.e goto S2;
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
        [OnEventGotoState(typeof(Foo.e), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with {}
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
        [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithAsyncBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with async { await Task.Delay(42); }
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
        [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action_async))]
        class S1 : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_S1_e_action_async()
        { await Task.Delay(42); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnSimpleGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> goto S2 with {}
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
        [OnEventGotoState(typeof(e<int>), typeof(S2), nameof(psharp_S1_e_type_0_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_type_0_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnComplexGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2 with {}
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
        [OnEventGotoState(typeof(e<List<Tuple<bool, object>>, Dictionary<string, float>>), typeof(S2), nameof(psharp_S1_e_type_0_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_type_0_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOMultipleGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with {}
on e<int> goto S2 with {}
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2 with {}
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
        [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action))]
        [OnEventGotoState(typeof(e<int>), typeof(S2), nameof(psharp_S1_e_type_1_action))]
        [OnEventGotoState(typeof(e<List<Tuple<bool, object>>, Dictionary<string, float>>), typeof(S2), nameof(psharp_S1_e_type_2_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_action()
        {}

        protected void psharp_S1_e_type_1_action()
        {}

        protected void psharp_S1_e_type_2_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar;
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
        [OnEventDoAction(typeof(e), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 do Bar;
on e2 do Baz;
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
        [OnEventDoAction(typeof(e1), nameof(Bar))]
        [OnEventDoAction(typeof(e2), nameof(Baz))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnSimpleGenericEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> do Bar;
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
        [OnEventDoAction(typeof(e<int>), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnComplexGenericEventDoActionDeclaration()
        {
            var test = @"
using System.Collection.Generic;
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> do Bar;
}
}
}";
            var expected = @"
using Microsoft.PSharp;
using System.Collection.Generic;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(e<List<Tuple<bool, object>>, Dictionary<string, float>>), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnQualifiedEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on Foo.e do Bar;
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
        [OnEventDoAction(typeof(Foo.e), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do {}
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
        [OnEventDoAction(typeof(e), nameof(psharp_S1_e_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithAsyncBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do async { await Task.Delay(42); }
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
        [OnEventDoAction(typeof(e), nameof(psharp_S1_e_action_async))]
        class S1 : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_S1_e_action_async()
        { await Task.Delay(42); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 goto S2;
on e2 do Bar;
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
        [OnEventGotoState(typeof(e1), typeof(S2))]
        [OnEventDoAction(typeof(e2), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestIgnoreEventDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e;
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
        [IgnoreEvents(typeof(e))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestIgnoreEventDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1, e2;
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
        [IgnoreEvents(typeof(e1), typeof(e2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestIgnoreEventDeclarationQualifiedComplex()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1<int>, Foo.e2<Bar.e3>;
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
        [IgnoreEvents(typeof(e1<int>), typeof(Foo.e2<Bar.e3>))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestDeferEventDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e;
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
        [DeferEvents(typeof(e))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestDeferEventDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1,e2;
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
        [DeferEvents(typeof(e1), typeof(e2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestDeferEventDeclarationQualifiedComplex()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1<int>, halt, default, Foo.e2<Bar.e3>;
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
        [DeferEvents(typeof(e1<int>), typeof(Microsoft.PSharp.Halt), typeof(Microsoft.PSharp.Default), typeof(Foo.e2<Bar.e3>))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestDefaultEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on default goto S2;
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
        [OnEventGotoState(typeof(Microsoft.PSharp.Default), typeof(S2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestHaltEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on halt goto S2;
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
        [OnEventGotoState(typeof(Microsoft.PSharp.Halt), typeof(S2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestWildcardEventDefer()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer *;
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
        [DeferEvents(typeof(Microsoft.PSharp.WildCardEvent))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestWildcardEventAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on *,e1.e2 goto S2;
on * push S2;
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
        [OnEventGotoState(typeof(Microsoft.PSharp.WildCardEvent), typeof(S2))]
        [OnEventGotoState(typeof(e1.e2), typeof(S2))]
        [OnEventPushState(typeof(Microsoft.PSharp.WildCardEvent), typeof(S2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestMultipleEventsSameAnonymousDo()
        {
            var test = @"
namespace Foo {
    machine M {
        start state S
        {
            on E1, E2, E3 do {
                assert(false, ""handler for all 3 events"");
            }
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
        [OnEventDoAction(typeof(E1), nameof(psharp_S_E1_action))]
        [OnEventDoAction(typeof(E2), nameof(psharp_S_E2_action))]
        [OnEventDoAction(typeof(E3), nameof(psharp_S_E3_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_E1_action()
        {
            this.Assert(false, ""handler for all 3 events"");
        }

        protected void psharp_S_E2_action()
        {
            this.Assert(false, ""handler for all 3 events"");
        }

        protected void psharp_S_E3_action()
        {
            this.Assert(false, ""handler for all 3 events"");
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestMultipleEventsSameNamedDo()
        {
            var test = @"
namespace Foo {
    machine M {
        start state S
        {
            on E1, E2, E3 do Check;
        }
        void Check() {
            assert(false, ""handler for all 3 events"");
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
        [OnEventDoAction(typeof(E1), nameof(Check))]
        [OnEventDoAction(typeof(E2), nameof(Check))]
        [OnEventDoAction(typeof(E3), nameof(Check))]
        class S : MachineState
        {
        }

        void Check()
        {
            this.Assert(false, ""handler for all 3 events"");
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestStateDeclarationWithMoreThanOneEntry()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
entry{}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate entry declaration.", test);
        }

        [Fact(Timeout=5000)]
        public void TestStateDeclarationWithMoreThanOneExit()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
exit {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate exit declaration.", test);
        }

        [Fact(Timeout=5000)]
        public void TestEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry Bar {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithoutEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on goto S2
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithCommaError()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e, goto S2
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected state identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on <> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventGotoStateDeclarationWithGenericError3()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<List<int>>> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithoutEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithCommaError()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e, do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionNamedHandlerWithAwaitedAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do await foo;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("'await' should not be used on actions.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionAnonymousHandlerWithAwaitedAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do await {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("'await' should not be used on actions.", test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncOnMachine()
        {
            var test = @"
namespace Foo {
async machine M {
start state S1
{
on e do {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncOnState()
        {
            var test = @"
namespace Foo {
machine M {
async state S1
{
on e do {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state cannot be async.", test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncInWrongDoLocation()
        {
            var test = @"
namespace Foo {
machine M {
state S1
{
async on e do {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("'async' was used in an incorrect context.", test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncInWrongEntryLocation()
        {
            var test = @"
namespace Foo {
machine M {
state S1
{
    entry async {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestAsyncInWrongExitLocation()
        {
            var test = @"
namespace Foo {
machine M {
state S1
{
    exit async {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithoutAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected async keyword, action identifier, or opening curly bracket.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithIncorrectWildcardUse()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e.* do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on <> do Bar;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e> do Bar;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDoActionDeclarationWithGenericError3()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<List<int>>> do Bar;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact(Timeout=5000)]
        public void TestOnEventDeclarationWithoutHandler()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"do\", \"goto\" or \"push\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestIgnoreEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1 e2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestDeferEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1 e2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestQualifiedHaltEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on Foo.halt goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestQualifiedDefaultEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on Foo.default goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        [Fact(Timeout=5000)]
        public void TestGenericHaltEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on halt<int> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact(Timeout=5000)]
        public void TestGenericDefaultEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on default<int> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [Fact(Timeout=5000)]
        public void TestIncorrectGenericEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on e<<int> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token inside a generic name.", test);
        }
    }
}
