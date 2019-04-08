// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class NameofTest : BaseTest
    {
        public NameofTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static int WithNameofValue;
        private static int WithoutNameofValue;

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class M_With_nameof : Machine
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(PSharp_Init_on_entry_action))]
            [OnExit(nameof(PSharp_Init_on_exit_action))]
            [OnEventGotoState(typeof(E1), typeof(Next), nameof(PSharp_Init_E1_action))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(PSharp_Next_on_entry_action))]
            [OnEventDoAction(typeof(E2), nameof(PSharp_Next_E2_action))]
            private class Next : MachineState
            {
            }

            protected void PSharp_Init_on_entry_action()
            {
                WithNameofValue += 1;
                this.Raise(new E1());
            }

            protected void PSharp_Init_on_exit_action()
            {
                WithNameofValue += 10;
            }

            protected void PSharp_Next_on_entry_action()
            {
                WithNameofValue += 1000;
                this.Raise(new E2());
            }

            protected void PSharp_Init_E1_action()
            {
                WithNameofValue += 100;
            }

            protected void PSharp_Next_E2_action()
            {
                WithNameofValue += 10000;
            }
        }

        private class M_Without_nameof : Machine
        {
            [Microsoft.PSharp.Start]
            [OnEntry("PSharp_Init_on_entry_action")]
            [OnExit("PSharp_Init_on_exit_action")]
            [OnEventGotoState(typeof(E1), typeof(Next), "PSharp_Init_E1_action")]
            private class Init : MachineState
            {
            }

            [OnEntry("PSharp_Next_on_entry_action")]
            [OnEventDoAction(typeof(E2), "PSharp_Next_E2_action")]
            private class Next : MachineState
            {
            }

            protected void PSharp_Init_on_entry_action()
            {
                WithoutNameofValue += 1;
                this.Raise(new E1());
            }

            protected void PSharp_Init_on_exit_action()
            {
                WithoutNameofValue += 10;
            }

            protected void PSharp_Next_on_entry_action()
            {
                WithoutNameofValue += 1000;
                this.Raise(new E2());
            }

            protected void PSharp_Init_E1_action()
            {
                WithoutNameofValue += 100;
            }

            protected void PSharp_Next_E2_action()
            {
                WithoutNameofValue += 10000;
            }
        }

        [Fact(Timeout=5000)]
        public void TestAllNameofWithNameof()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M_With_nameof));
            });

            this.AssertSucceeded(test);
            Assert.Equal(11111, WithNameofValue);
        }

        [Fact(Timeout=5000)]
        public void TestAllNameofWithoutNameof()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M_Without_nameof));
            });

            this.AssertSucceeded(test);
            Assert.Equal(11111, WithoutNameofValue);
        }
    }
}
