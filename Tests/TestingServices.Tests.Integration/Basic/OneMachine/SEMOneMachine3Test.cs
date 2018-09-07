// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine3Test : BaseTest
    {
        class E2 : Event
        {
            public E2() : base(1, -1) { }
        }

        class Real1 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventDoAction(typeof(E2), nameof(Action2))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E2());
            }

            void Action2()
            {
                this.Assert(false);  // fails here
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "send" to itself in entry actions.
        /// </summary>
        [Fact]
        public void TestSendInEntry()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
