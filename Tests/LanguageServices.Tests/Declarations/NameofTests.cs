﻿// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class NameofTests
    {
        private readonly string NameofTest = @"
namespace Foo {
    machine M_with_nameof {
        start state Init
        {
            entry
            {
                Value += 1;
                raise(E1);
            }
            exit { Value += 10; }
            on E1 goto Next with { Value += 100; }
        }
        
        state Next
        {
            entry
            {
                Value += 1000;
                raise(E2);
            }
            on E2 do { Value += 10000; } 
        }
    }
}";

        [Fact(Timeout=5000)]
        public void TestAllNameofRewriteWithNameof()
        {
            var test = this.NameofTest;
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M_with_nameof : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        [OnExit(nameof(psharp_Init_on_exit_action))]
        [OnEventGotoState(typeof(E1), typeof(Next), nameof(psharp_Init_E1_action))]
        class Init : MachineState
        {
        }

        [OnEntry(nameof(psharp_Next_on_entry_action))]
        [OnEventDoAction(typeof(E2), nameof(psharp_Next_E2_action))]
        class Next : MachineState
        {
        }

        protected void psharp_Init_on_entry_action()
        {
            Value += 1;
            this.Raise(new E1());
        }

        protected void psharp_Init_on_exit_action()
        { Value += 10; }

        protected void psharp_Next_on_entry_action()
        {
            Value += 1000;
            this.Raise(new E2());
        }

        protected void psharp_Init_E1_action()
        { Value += 100; }

        protected void psharp_Next_E2_action()
        { Value += 10000; }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestAllNameofRewriteWithoutNameof()
        {
            var test = this.NameofTest;
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M_with_nameof : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(""psharp_Init_on_entry_action"")]
        [OnExit(""psharp_Init_on_exit_action"")]
        [OnEventGotoState(typeof(E1), typeof(Next), ""psharp_Init_E1_action"")]
        class Init : MachineState
        {
        }

        [OnEntry(""psharp_Next_on_entry_action"")]
        [OnEventDoAction(typeof(E2), ""psharp_Next_E2_action"")]
        class Next : MachineState
        {
        }

        protected void psharp_Init_on_entry_action()
        {
            Value += 1;
            this.Raise(new E1());
        }

        protected void psharp_Init_on_exit_action()
        { Value += 10; }

        protected void psharp_Next_on_entry_action()
        {
            Value += 1000;
            this.Raise(new E2());
        }

        protected void psharp_Init_E1_action()
        { Value += 100; }

        protected void psharp_Next_E2_action()
        { Value += 10000; }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test, csVersion: new System.Version(5, 0));
        }
    }
}
