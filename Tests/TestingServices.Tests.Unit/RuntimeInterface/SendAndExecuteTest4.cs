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
    public class SendAndExecuteTest4 : BaseTest
    {
        public SendAndExecuteTest4(ITestOutputHelper output)
            : base(output)
        { }

        class LE : Event { }

        class E : Event
        {
            public MachineId mid;

            public E(MachineId mid)
            {
                this.mid = mid;
            }
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var runtime = this.Id.Runtime;
                var m = await runtime.CreateMachineAndExecuteAsync(typeof(M), new E(this.Id));
                var handled = await runtime.SendEventAndExecuteAsync(m, new LE());
                this.Assert(handled);
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E).mid;
                var runtime = this.Id.Runtime;
                var handled = await runtime.SendEventAndExecuteAsync(creator, new LE());
                this.Assert(!handled);
            }

        }


        [Fact]
        public void TestSendCycleDoesNotDeadlock()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertSucceeded(config, test);
        }
    }
}
