// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Deprecated.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Deprecated
{
    public class DeprecatedIllegalPeriodTest : BaseTest
    {
        public DeprecatedIllegalPeriodTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class T4 : TimedMachine
        {
            private readonly object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private async Task Initialize()
            {
                // Incorrect period, will throw assertion violation
                TimerId tid = this.StartTimer(this.payload, -1, true);
                await this.StopTimer(tid, flush: true);
            }
        }

        [Fact]
        public void IllegalTimerStopTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(T4));
            });
            this.AssertFailed(test, 1, true);
        }
    }
}
