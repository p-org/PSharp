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
    public class AsyncAwaitTest : BaseTest
    {
        public AsyncAwaitTest(ITestOutputHelper output)
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

        [Fact]
        public void TestAsyncDelay()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestAsyncDelayWithOtherSynchronizationContext()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(N));
            });

            var bugReport = "Detected synchronization context that is not controlled by the P# runtime.";
            this.AssertFailed(test, bugReport, true);
        }
    }
}
