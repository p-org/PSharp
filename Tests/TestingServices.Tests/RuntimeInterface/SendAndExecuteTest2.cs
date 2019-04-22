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
    public class SendAndExecuteTest2 : BaseTest
    {
        public SendAndExecuteTest2(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
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
                var b = this.CreateMachine(typeof(B));
                var handled = await this.Runtime.SendEventAndExecute(b, new E1());
                this.Assert(!handled);
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

        private class C : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var d = this.CreateMachine(typeof(D));
                var handled = await this.Runtime.SendEventAndExecute(d, new E1());
                this.Assert(handled);
            }
        }

        private class D : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            private void Handle()
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestSyncSendToReceive()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(A));
            });

            this.AssertSucceeded(config, test);
        }

        [Fact(Timeout=5000)]
        public void TestSyncSendSometimesDoesNotHandle()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(C));
            });

            this.AssertFailed(config, test, 1, true);
        }
    }
}
