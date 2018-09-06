// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine1Test : BaseTest
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
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            [OnEventDoAction(typeof(E2), nameof(Action2))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E2());
                this.Raise(new E1());
            }

            void Action1()
            {
                test = true;
            }

            void Action2()
            {
                this.Assert(test == false);  // fails here
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "send" to itself and then
        /// "raise" in entry actions.
        /// </summary>
        [Fact]
        public void TestSendRaiseInEntry()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
