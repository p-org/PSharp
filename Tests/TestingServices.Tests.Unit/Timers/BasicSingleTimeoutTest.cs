// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class BasicSingleTimeoutTest : BaseTest
    {
        public BasicSingleTimeoutTest(ITestOutputHelper output)
            : base(output)
        { }

        private class T1 : TimedMachine
        {
            TimerId tid;
            object payload = new object();
            int count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                count = 0;

                // Start a one-off timer.
                tid = StartTimer(payload, 10, false);
            }

            void HandleTimeout()
            {
                count++;
                this.Assert(count == 1);
            }
        }

        [Fact]
        public void SingleTimeoutTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(T1));
            });

            base.AssertSucceeded(test);
        }
    }
}
