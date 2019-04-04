// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.Deprecated.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Deprecated
{
    public class DeprecatedBasicSingleTimeoutTest : BaseTest
    {
        public DeprecatedBasicSingleTimeoutTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class T1 : TimedMachine
        {
            private TimerId tid;
            private readonly object payload = new object();
            private int count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.count = 0;

                // Start a one-off timer.
                this.tid = this.StartTimer(this.payload, 10, false);
            }

            private void HandleTimeout()
            {
                this.count++;
                this.Assert(this.count == 1);
            }
        }

        [Fact]
        public void SingleTimeoutTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(T1));
            });

            this.AssertSucceeded(test);
        }
    }
}
