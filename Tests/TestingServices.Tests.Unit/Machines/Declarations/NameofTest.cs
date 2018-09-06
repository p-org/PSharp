// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class NameofTest : BaseTest
    {
        static int WithNameofValue;
        static int WithoutNameofValue;

        class E1 : Event { }
        class E2 : Event { }

        class M_With_nameof : Machine
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
                WithNameofValue += 1;
                this.Raise(new E1());
            }

            protected void psharp_Init_on_exit_action()
            { WithNameofValue += 10; }

            protected void psharp_Next_on_entry_action()
            {
                WithNameofValue += 1000;
                this.Raise(new E2());
            }

            protected void psharp_Init_E1_action()
            { WithNameofValue += 100; }

            protected void psharp_Next_E2_action()
            { WithNameofValue += 10000; }
        }

        class M_Without_nameof : Machine
        {
            [Microsoft.PSharp.Start]
            [OnEntry("psharp_Init_on_entry_action")]
            [OnExit("psharp_Init_on_exit_action")]
            [OnEventGotoState(typeof(E1), typeof(Next), "psharp_Init_E1_action")]
            class Init : MachineState
            {
            }

            [OnEntry("psharp_Next_on_entry_action")]
            [OnEventDoAction(typeof(E2), "psharp_Next_E2_action")]
            class Next : MachineState
            {
            }

            protected void psharp_Init_on_entry_action()
            {
                WithoutNameofValue += 1;
                this.Raise(new E1());
            }

            protected void psharp_Init_on_exit_action()
            { WithoutNameofValue += 10; }

            protected void psharp_Next_on_entry_action()
            {
                WithoutNameofValue += 1000;
                this.Raise(new E2());
            }

            protected void psharp_Init_E1_action()
            { WithoutNameofValue += 100; }

            protected void psharp_Next_E2_action()
            { WithoutNameofValue += 10000; }
        }

        [Fact]
        public void TestAllNameofWithNameof()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M_With_nameof));
            });

            base.AssertSucceeded(test);
            Assert.Equal(11111, WithNameofValue);
        }

        [Fact]
        public void TestAllNameofWithoutNameof()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M_Without_nameof));
            });

            base.AssertSucceeded(test);
            Assert.Equal(11111, WithoutNameofValue);
        }
    }
}
