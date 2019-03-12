// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TimerLivenessTest : BaseTest
    {
        public TimerLivenessTest(ITestOutputHelper output)
            : base(output)
        { }

        class TimeoutReceivedEvent : Event { }

        class Client : TimedMachine
        {
            TimerId tid;
            object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState { }

            private void Initialize()
            {
                tid = StartTimer(payload, 10, false);
            }

            private void HandleTimeout()
            {
                this.Monitor<LivenessMonitor>(new TimeoutReceivedEvent());
            }
        }

        class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(TimeoutReceivedEvent), typeof(TimeoutReceived))]
            class NoTimeoutReceived : MonitorState { }

            [Cold]
            class TimeoutReceived : MonitorState { }
        }

        [Fact]
        public void PeriodicLivenessTest()
        {
            var config = base.GetConfiguration();
            config.LivenessTemperatureThreshold = 150;
            config.MaxSchedulingSteps = 300;
            config.SchedulingIterations = 1000;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Client));
            });

            base.AssertSucceeded(config, test);
        }
    }
}
