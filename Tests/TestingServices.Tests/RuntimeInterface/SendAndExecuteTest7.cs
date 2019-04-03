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
        {
        }

        private class E : Event
        {
        }

        private class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Assert(handled);
            }
        }

        private class M : Machine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact]
        public void TestUnhandledEventOnSendExec()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(Harness));
            });

            this.AssertFailed(test, "Machine 'M()' received event 'E' that cannot be handled.", true);
        }
    }
}
