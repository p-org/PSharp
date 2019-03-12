// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class SendAndExecuteTest7 : BaseTest
    {
        public SendAndExecuteTest7(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Assert(handled);
            }

        }

        class M : Machine
        {

            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestUnhandledEventOnSendExec()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });

            base.AssertFailed(test, "Machine 'M()' received event 'E' that cannot be handled.", true);
        }

    }
}
