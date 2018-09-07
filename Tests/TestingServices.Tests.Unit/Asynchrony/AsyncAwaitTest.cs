﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class AsyncAwaitTest : BaseTest
    {
        public AsyncAwaitTest(ITestOutputHelper output)
               : base(output)
        { }

        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            async Task EntryInit()
            {
                this.Send(this.Id, new E());
                await Task.Delay(2);
                this.Send(this.Id, new E());
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            async Task EntryInit()
            {
                this.Send(this.Id, new E());
                await Task.Delay(20).ConfigureAwait(false);
                this.Send(this.Id, new E());
            }
        }

        [Fact]
        public void TestAsyncDelay()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestAsyncDelayWithOtherSynchronizationContext()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(N));
            });

            var bugReport = "Detected synchronization context that is not controlled by the P# runtime.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
