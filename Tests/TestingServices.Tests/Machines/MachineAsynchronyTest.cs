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
    public class MachineAsynchronyTest : BaseTest
    {
        public MachineAsynchronyTest(ITestOutputHelper output)
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
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await Task.Delay(2);
                this.Send(this.Id, new E());
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await Task.Delay(20).ConfigureAwait(false);
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestAsyncDelay()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M));
            });
        }

        [Fact(Timeout=5000)]
        public void TestAsyncDelayWithOtherSynchronizationContext()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(N));
            },
            expectedError: "Detected concurrency that is not controlled by the P# runtime.",
            replay: true);
        }
    }
}
