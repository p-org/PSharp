// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
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
                var runtime = this.Id.Runtime;
                var m = await runtime.CreateMachineAndExecuteAsync(typeof(M));
                var handled = await runtime.SendEventAndExecuteAsync(m, new E());
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
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });

            base.AssertFailed(test, "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.SendAndExecuteTest7+M()' received event 'Microsoft.PSharp.TestingServices.Tests.Unit.SendAndExecuteTest7+E' that cannot be handled.", true);
        }
    }
}
