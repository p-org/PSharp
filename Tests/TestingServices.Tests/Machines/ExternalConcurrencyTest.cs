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
    public class ExternalConcurrencyInMachineTest : BaseTest
    {
        public ExternalConcurrencyInMachineTest(ITestOutputHelper output)
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

            private async Task InitOnEntry()
            {
                await Task.Run(() =>
                {
                    this.Send(this.Id, new E());
                });
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.Run(() =>
                {
                    this.Random();
                });
            }
        }

        [Fact(Timeout=5000)]
        public void TestExternalTaskSendingEvent()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M));
            },
            expectedError: "Detected task with id '' that is not controlled by the P# runtime.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestExternalTaskInvokingRandom()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(N));
            },
            expectedError: "Detected task with id '' that is not controlled by the P# runtime.",
            replay: true);
        }
    }
}
