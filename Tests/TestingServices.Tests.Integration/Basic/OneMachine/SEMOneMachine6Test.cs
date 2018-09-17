// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine6Test : BaseTest
    {
        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public E2() : base(1, -1) { }
        }

        class Real1 : Machine
        {
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E1());
            }

            void ExitInit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventDoAction(typeof(E2), nameof(Action2))]
            class S1 : MachineState { }

            void EntryS1()
            {
                test = true;
            }

            void Action2()
            {
                this.Assert(test == false);  // reachable
            }
        }

        /// <summary>
        /// P# semantics test: one machine; "send" to itself in exit function.
        /// E2 is sent upon executing goto; however, E2 is handled in S1 state
        /// by Action2.
        /// </summary>
        [Fact]
        public void TestSendInExitHandledEvent()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
