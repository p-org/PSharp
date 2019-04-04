// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Deprecated.Timers;
using Microsoft.PSharp.TestingServices.Deprecated.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Deprecated
{
    public class DeprecatedBasicPeriodicTimeoutTest : BaseTest
    {
        public DeprecatedBasicPeriodicTimeoutTest(ITestOutputHelper output)
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

                // Start a periodic timer.
                this.tid = this.StartTimer(this.payload, 10, true);
            }

            private async Task HandleTimeout()
            {
                this.count++;
                this.Assert(this.count <= 10);

                if (this.count == 10)
                {
                    await this.StopTimer(this.tid, flush: true);
                }
            }
        }

        [Fact]
        public void PeriodicTimeoutTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            ModelTimerMachine.NumStepsToSkip = 1;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(T1));
            });
            this.AssertSucceeded(test);
        }
    }
}
