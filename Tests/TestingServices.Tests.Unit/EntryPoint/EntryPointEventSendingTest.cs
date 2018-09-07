// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class EntryPointEventSendingTest : BaseTest
    {
        public EntryPointEventSendingTest(ITestOutputHelper output)
            : base(output)
        { }

        class Transfer : Event
        {
            public int Value;

            public Transfer(int value)
            {
                this.Value = value;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Transfer), nameof(HandleTransfer))]
            class Init : MachineState { }

            void HandleTransfer()
            {
                int value = (this.ReceivedEvent as Transfer).Value;
                this.Assert(value > 0, "Value is 0.");
            }
        }

        [Fact]
        public void TestEntryPointEventSending()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                MachineId m = r.CreateMachine(typeof(M));
                r.SendEvent(m, new Transfer(0));
            });

            var bugReport = "Value is 0.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
