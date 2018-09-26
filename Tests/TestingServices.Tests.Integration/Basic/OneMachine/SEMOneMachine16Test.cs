// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine16Test : BaseTest
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
            [OnEventPushState(typeof(E1), typeof(S1))]
            [OnEventDoAction(typeof(E3), nameof(Action1))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E1());
            }

            void ExitInit()
            {
                this.Send(this.Id, new E2()); // never executed
            }

            [OnEntry(nameof(EntryS1))]
            [OnExit(nameof(ExitS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                test = true;
                this.Pop();
            }

            void ExitS1()
            {
                this.Send(this.Id, new E3());
            }

            void Action1()
            {
                this.Assert(test == false);  // reachable
            }
        }

        /// <summary>
        /// P# semantics test: one machine, exit actions executed upon explicit "pop".
        /// This test checks that when the state is explicitly popped, exit function
        /// of that state is executed.
        /// </summary>
        [Fact]
        public void TestExplicitPopExit()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
