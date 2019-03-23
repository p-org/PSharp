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
    public class SendAndExecuteTest4 : BaseTest
    {
        public SendAndExecuteTest4(ITestOutputHelper output)
            : base(output)
        {
        }

        private class LE : Event
        {
        }

        private class E : Event
        {
            public MachineId Mid;

            public E(MachineId mid)
            {
                this.Mid = mid;
            }
        }

        private class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M), new E(this.Id));
                var handled = await this.Runtime.SendEventAndExecute(m, new LE());
                this.Assert(handled);
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E).Mid;
                var handled = await this.Id.Runtime.SendEventAndExecute(creator, new LE());
                this.Assert(!handled);
            }
        }

        [Fact]
        public void TestSendCycleDoesNotDeadlock()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(Harness));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            this.AssertSucceeded(config, test);
        }
    }
}
