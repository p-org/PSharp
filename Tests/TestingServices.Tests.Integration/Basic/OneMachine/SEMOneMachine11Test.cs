// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine11Test : BaseTest
    {
        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public E2() : base(1, -1) { }
        }

        class E3 : Event
        {
            public E3() : base(1, -1) { }
        }

        class Real1 : Machine
        {
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
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
            class S1 : MachineState { }

            void EntryS1()
            {
                test = true;
                this.Send(this.Id, new E3());
                // at this point, the queue is: E1; E3; Real1_S1 pops and Real1_Init is re-entered
            }

            void Action1()
            {
                this.Assert(test == false);  // reachable
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "push" with implicit "pop" when
        /// the unhandled event was sent. This test checks implicit popping
        /// of the state when there's an unhandled event which was sent to
        /// the queue. Also, if a state is re-entered (meaning that the state
        /// was already on the stack), entry function is not executed.
        /// </summary>
        [Fact]
        public void TestPushImplicitPopWithSend()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
