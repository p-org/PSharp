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
    public class SendAndExecuteTest3 : BaseTest
    {
        public SendAndExecuteTest3(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public int X;

            public E()
            {
                this.X = 0;
            }
        }

        private class LE : Event
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
                var e = new E();
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, e);
                this.Assert(handled);
                this.Assert(e.X == 1);
            }
        }

        private class M : Machine
        {
            private bool LEHandled = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleEventE))]
            [OnEventDoAction(typeof(LE), nameof(HandleEventLE))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new LE());
            }

            private void HandleEventLE()
            {
                this.LEHandled = true;
            }

            private void HandleEventE()
            {
                this.Assert(this.LEHandled);
                var e = this.ReceivedEvent as E;
                e.X = 1;
            }
        }

        [Fact]
        public void TestSendBlocks()
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
