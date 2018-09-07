// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine32Test : BaseTest
    {
        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class Real1 : Machine
        {
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventPushState(typeof(Halt), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action2))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E1());
                this.Raise(new Halt());
            }

            void ExitInit() { }

            [OnEntry(nameof(EntryS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                test = true;
            }

            void Action2()
            {
                this.Assert(test == false); // reachable
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "halt" is raised and handled.
        /// </summary>
        [Fact]
        public void TestRaiseHaltHandled()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
