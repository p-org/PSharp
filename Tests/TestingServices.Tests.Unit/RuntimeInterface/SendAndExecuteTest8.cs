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
    public class SendAndExecuteTest8 : BaseTest
    {
        public SendAndExecuteTest8(ITestOutputHelper output)
            : base(output)
        { }

        class E1 : Event { }
        class E2 : Event { }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, new E1());
                this.Assert(handled);
            }
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            [IgnoreEvents(typeof(E2))]
            class Init : MachineState { }

            void Handle()
            {
                this.Raise(new E2());
            }
        }

        [Fact]
        public void TestUnhandledEventOnSendExec()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });

            base.AssertSucceeded(test);
        }
    }
}
