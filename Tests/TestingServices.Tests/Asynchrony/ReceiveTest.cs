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
    public class ReceiveTest : BaseTest
    {
        public ReceiveTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await this.Receive(typeof(E));
                this.Assert(false);
            }
        }

        [Fact]
        public void TestAsyncReceiveEvent()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            var bugReport = "Detected an assertion failure.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
