// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class IllegalPeriodTest : BaseTest
    {
        public IllegalPeriodTest(ITestOutputHelper output)
            : base(output)
        { }
        private class T4 : TimedMachine
        {
            object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }
            async Task Initialize()
            {
                // Incorrect period, will throw assertion violation
                TimerId tid = this.StartTimer(payload, -1, true);
                await this.StopTimer(tid, flush: true);
            }
        }

        [Fact]
        public void IllegalTimerStopTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(T4));
            });
            base.AssertFailed(test, 1, true);
        }
    }
}
