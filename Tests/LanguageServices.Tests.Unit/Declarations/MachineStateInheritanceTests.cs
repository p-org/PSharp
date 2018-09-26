// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class MachineStateInheritanceTests
    {
        #region correct tests

        [Fact]
        public void TestAbstractBaseStateWithOnEventDeclaration()
        {
            var test = @"
namespace Foo {
    machine Machine1 {
        start state Init : BaseState {
            entry {
                send(this.Id, E);
            }
        }

        abstract state BaseState {
            on E do Check;
        }

        void Check() {
            assert(false, ""Check reached."")
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class Machine1 : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        class Init : BaseState
        {
        }

        [OnEventDoAction(typeof(E), nameof(Check))]
        abstract class BaseState : MachineState
        {
        }

        void Check()
        {
            this.Assert(false, ""Check reached."")
        }

        protected void psharp_Init_on_entry_action()
        {
            this.Send(this.Id,new E());
        }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestBaseStateWithOnEventDeclaration()
        {
            var test = @"
namespace Foo {
    event E;
    
    machine Machine1 {
        start state Init : BaseState {
            entry {
                send(this.Id, E);
            }
        }

        state BaseState {
            on E do Check;
        }

        void Check() {
            assert(false, ""Check reached."")
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class E : Event
    {
        public E()
            : base()
        {
        }
    }

    class Machine1 : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        class Init : BaseState
        {
        }

        [OnEventDoAction(typeof(E), nameof(Check))]
        class BaseState : MachineState
        {
        }

        void Check()
        {
            this.Assert(false, ""Check reached."")
        }

        protected void psharp_Init_on_entry_action()
        {
            this.Send(this.Id,new E());
        }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestBaseAndDerivedOnEntry()
        {
            var test = @"
namespace Foo {
    event E1;
    event E2;
    
    machine Machine1 {
        start state Init : BaseState {
            entry {
                send(this.Id, E1);
            }
        }

        abstract state BaseState {
            entry {
                send(this.Id, E2);
            }
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class E1 : Event
    {
        public E1()
            : base()
        {
        }
    }

    public class E2 : Event
    {
        public E2()
            : base()
        {
        }
    }

    class Machine1 : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        class Init : BaseState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action))]
        abstract class BaseState : MachineState
        {
        }

        protected void psharp_Init_on_entry_action()
        {
            this.Send(this.Id,new E1());
        }

        protected void psharp_BaseState_on_entry_action()
        {
            this.Send(this.Id,new E2());
        }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestBaseAndDerivedEachWithOnEntryAndDoAction()
        {
            var test = @"
namespace Foo {
    machine Machine1 {
        start state Init : BaseState {
            entry {
                send(this.Id, E1);
            }
            on E1 do Check1;
        }

        abstract state BaseState {
            entry {
                send(this.Id, E2);
            }
            on E2 do Check2;
        }

        void Check1() {
            assert(false, ""Check1 reached."")
        }

        void Check2() {
            assert(false, ""Check2 reached."")
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class Machine1 : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        [OnEventDoAction(typeof(E1), nameof(Check1))]
        class Init : BaseState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action))]
        [OnEventDoAction(typeof(E2), nameof(Check2))]
        abstract class BaseState : MachineState
        {
        }

        void Check1()
        {
            this.Assert(false, ""Check1 reached."")
        }

        void Check2()
        {
            this.Assert(false, ""Check2 reached."")
        }

        protected void psharp_Init_on_entry_action()
        {
            this.Send(this.Id,new E1());
        }

        protected void psharp_BaseState_on_entry_action()
        {
            this.Send(this.Id,new E2());
        }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestThirdUnderivedState()
        {
            var test = @"
namespace Foo {
    machine Machine1 {
        start state SecondState : BaseState {
            entry { send(this.Id, E1); }
        }

        state BaseState {
            entry { send(this.Id, E2); }
        }

        state ThirdState : SecondState {
            entry { send(this.Id, E3); }
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class Machine1 : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_SecondState_on_entry_action))]
        class SecondState : BaseState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action))]
        class BaseState : MachineState
        {
        }

        [OnEntry(nameof(psharp_ThirdState_on_entry_action))]
        class ThirdState : SecondState
        {
        }

        protected void psharp_SecondState_on_entry_action()
        { this.Send(this.Id,new E1()); }

        protected void psharp_BaseState_on_entry_action()
        { this.Send(this.Id,new E2()); }

        protected void psharp_ThirdState_on_entry_action()
        { this.Send(this.Id,new E3()); }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestNestedDerivedState()
        {
            var test = @"
namespace Foo {
    machine Machine1 {
        start state Init : ThirdState {
            entry {
                send(this.Id, E1);
            }
        }

        state BaseState {
            entry {
                send(this.Id, E2);
            }
        }

        state ThirdState : BaseState {
            entry {
                send(this.Id, E3);
            }
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class Machine1 : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        class Init : ThirdState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action))]
        class BaseState : MachineState
        {
        }

        [OnEntry(nameof(psharp_ThirdState_on_entry_action))]
        class ThirdState : BaseState
        {
        }

        protected void psharp_Init_on_entry_action()
        {
            this.Send(this.Id,new E1());
        }

        protected void psharp_BaseState_on_entry_action()
        {
            this.Send(this.Id,new E2());
        }

        protected void psharp_ThirdState_on_entry_action()
        {
            this.Send(this.Id,new E3());
        }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestBaseAsyncEntryDeclaration()
        {
            var test = @"
namespace Foo {
    machine M {
        start state Init : BaseState {
            entry { send(this.Id, E1); }
        }

        state BaseState {
            async entry { await Task.Delay(42); }
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
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        class Init : BaseState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action_async))]
        class BaseState : MachineState
        {
        }

        protected void psharp_Init_on_entry_action()
        { this.Send(this.Id,new E1()); }

        protected async System.Threading.Tasks.Task psharp_BaseState_on_entry_action_async()
        { await Task.Delay(42); }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestDerivedAsyncEntryDeclaration()
        {
            var test = @"
namespace Foo {
    machine M {
        start state Init : BaseState {
            async entry { await Task.Delay(42); }
        }

        state BaseState {
            entry { send(this.Id, E1); }
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
        [OnEntry(nameof(psharp_Init_on_entry_action_async))]
        class Init : BaseState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action))]
        class BaseState : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_Init_on_entry_action_async()
        { await Task.Delay(42); }

        protected void psharp_BaseState_on_entry_action()
        { this.Send(this.Id,new E1()); }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestBaseAndDerivedAsyncEntryDeclaration()
        {
            var test = @"
namespace Foo {
    machine M {
        start state Init : BaseState {
            async entry { await Task.Delay(42); }
        }

        state BaseState {
            async entry { await Task.Delay(43); }
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
        [OnEntry(nameof(psharp_Init_on_entry_action_async))]
        class Init : BaseState
        {
        }

        [OnEntry(nameof(psharp_BaseState_on_entry_action_async))]
        class BaseState : MachineState
        {
        }

        protected async System.Threading.Tasks.Task psharp_Init_on_entry_action_async()
        { await Task.Delay(42); }

        protected async System.Threading.Tasks.Task psharp_BaseState_on_entry_action_async()
        { await Task.Delay(43); }
    }
}";
            AssertWithEntryExitReplacement(expected, test);
        }

        [Fact]
        public void TestInheritedOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
    machine M {
        abstract state BaseState
        {
            on e goto BaseDest;
        }
        start state Init : BaseState
        {
            on e goto InitDest;
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [OnEventGotoState(typeof(e), typeof(BaseDest))]
        abstract class BaseState : MachineState
        {
        }

        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e), typeof(InitDest))]
        class Init : BaseState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }


        #endregion

        #region failure tests

        [Fact]
        public void TestOnlyOneStartState()
        {
            var test = @"
namespace Foo {
    machine Machine1 {
        start state Init : BaseState {
        }

        start state BaseState {
        }
    }
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine can declare only a single start state.", test);
        }

        #endregion

        #region utility methods
        private string ReplaceEntryWithExitInTest(string test)
        {
            return test.Replace("entry {", "exit {").Replace("entry{", "exit{");
        }

        private string ReplaceEntryWithExitInExpected(string expected)
        {
            return expected.Replace("OnEntry(", "OnExit(").Replace("_entry_", "_exit_");
        }

        private void AssertWithEntryExitReplacement(string expected, string test)
        {
            // Test both Entry and Exit combination
            LanguageTestUtilities.AssertRewritten(expected, test);
            LanguageTestUtilities.AssertRewritten(ReplaceEntryWithExitInExpected(expected), ReplaceEntryWithExitInTest(test));
        }
        #endregion
    }
}
