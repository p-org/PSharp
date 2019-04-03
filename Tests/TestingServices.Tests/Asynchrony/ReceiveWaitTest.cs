// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class ReceiveWaitTest : BaseTest
    {
        public ReceiveWaitTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
                this.Receive(typeof(E)).Wait();
                this.Assert(false);
            }
        }

        [Fact]
        public void TestAsyncReceiveEvent()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M));
            });

            var bugReport = "Detected an assertion failure.";
            this.AssertFailed(test, bugReport, true);
        }
    }
}
