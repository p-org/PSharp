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
    public class DeprecatedTimerLivenessTest : BaseTest
    {
        public DeprecatedTimerLivenessTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class TimeoutReceivedEvent : Event
        {
        }

        private class Client : TimedMachine
        {
            private TimerId tid;
            private readonly object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                this.tid = this.StartTimer(this.payload, 10, false);
            }

            private void HandleTimeout()
            {
                this.Monitor<LivenessMonitor>(new TimeoutReceivedEvent());
            }
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(TimeoutReceivedEvent), typeof(TimeoutReceived))]
            private class NoTimeoutReceived : MonitorState
            {
            }

            [Cold]
            private class TimeoutReceived : MonitorState
            {
            }
        }

        [Fact]
        public void PeriodicLivenessTest()
        {
            var config = GetConfiguration();
            config.LivenessTemperatureThreshold = 150;
            config.MaxSchedulingSteps = 300;
            config.SchedulingIterations = 1000;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Client));
            });

            this.AssertSucceeded(config, test);
        }
    }
}
