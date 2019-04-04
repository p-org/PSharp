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
        {
        }

        private class Configure : Event
        {
            public bool ExecuteSynchronously;

            public Configure(bool executeSynchronously)
            {
                this.ExecuteSynchronously = executeSynchronously;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
            {
                this.Id = id;
            }
        }

        private class E3 : Event
        {
        }

        private class A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var e = this.ReceivedEvent as Configure;
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

        private class B : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
            }
        }

        [Fact]
        public void TestSendAndExecuteNoDeadlockWithReceive()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(A), new Configure(false));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestSendAndExecuteDeadlockWithReceive()
        {
            var config = Configuration.Create().WithNumberOfIterations(10);
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(A), new Configure(true));
            });

            this.AssertFailed(config, test, "Livelock detected. 'A()' and 'B()' are waiting " +
                "for an event, but no other schedulable choices are enabled.", true);
        }
    }
}
