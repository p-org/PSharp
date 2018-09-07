// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine10Test : BaseTest
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
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(S1))]
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
                this.Assert(false); // reachable
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "push", "send" in entry and exit.
        /// This test checks that upon executing "goto" transition, exit
        /// function is executed, but upon executing "push" transition, exit
        /// function is not executed.
        /// </summary>
        [Fact]
        public void TestPush()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });
            
            base.AssertFailed(test, 1);
        }
    }
}
