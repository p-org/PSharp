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
    public class DeprecatedStartStopTimerTest : BaseTest
    {
        public DeprecatedStartStopTimerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class TimeoutReceivedEvent : Event
        {
        }

        private class Client : TimedMachine
        {
            private readonly object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
            {
            }

            private async Task Initialize()
            {
                // Start a timer, and stop it immediately.
                TimerId tid = this.StartTimer(this.payload, 10, true);
                await this.StopTimer(tid, flush: false);
            }

            // Timer fired in the interval between StartTimer and StopTimer.
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

        /// <summary>
        /// Test the fact that no timeouts may arrive between StartTimer and StopTimer.
        /// </summary>
        [Fact]
        public void StartStopTest()
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

            this.AssertFailed(config, test,
                "Monitor 'LivenessMonitor' detected liveness bug in hot state " +
                "'LivenessMonitor.NoTimeoutReceived' at the end of program execution.",
                true);
        }
    }
}
