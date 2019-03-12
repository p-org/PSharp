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
    public class SendAndExecuteTest1 : BaseTest
    {
        public SendAndExecuteTest1(ITestOutputHelper output)
            : base(output)
        { }

        class Configure : Event
        {
            public bool ExecuteSynchronously;

            public Configure(bool executeSynchronously)
            {
                this.ExecuteSynchronously = executeSynchronously;
            }
        }

        class E1 : Event { }
        class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
            {
                this.Id = id;
            }
        }
        class E3 : Event { }

        class A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var e = (this.ReceivedEvent as Configure);
                MachineId b;

                if (e.ExecuteSynchronously)
                {
                     b = await this.Runtime.CreateMachineAndExecute(typeof(B));
                }
                else
                {
                    b = this.Runtime.CreateMachine(typeof(B));
                }
                this.Send(b, new E1());
            }
        }

        class B : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
            }

        }

        [Fact]
        public void TestSendAndExecuteNoDeadlockWithReceive()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A), new Configure(false));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestSendAndExecuteDeadlockWithReceive()
        {
            var config = Configuration.Create().WithNumberOfIterations(10);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A), new Configure(true));
            });

            base.AssertFailed(config, test, "Livelock detected. 'A()' and 'B()' are waiting " +
                "for an event, but no other schedulable choices are enabled.", true);
        }
    }
}
