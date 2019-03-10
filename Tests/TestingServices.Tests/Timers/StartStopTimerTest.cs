// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class StartStopTimerTest : BaseTest
    {
        public StartStopTimerTest(ITestOutputHelper output)
            : base(output)
        { }

        class TimeoutReceivedEvent : Event { }

        class Client : TimedMachine
        {
            object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Init : MachineState { }

            async Task Initialize()
            {
                // Start a timer, and stop it immediately.
                TimerId tid = StartTimer(payload, 10, true);
                await this.StopTimer(tid, flush: false);
            }

            // Timer fired in the interval between StartTimer and StopTimer.
            void HandleTimeout()
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

        /// <summary>
        /// Test the fact that no timeouts may arrive between StartTimer and StopTimer.
        /// </summary>
        [Fact]
        public void StartStopTest()
        {
            var config = base.GetConfiguration();
            config.LivenessTemperatureThreshold = 150;
            config.MaxSchedulingSteps = 300;
            config.SchedulingIterations = 1000;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Client));
            });

            base.AssertFailed(config, test,
                "Monitor 'LivenessMonitor' detected liveness bug in hot state " +
                "'LivenessMonitor.NoTimeoutReceived' at the end of program execution.",
                true);
        }
    }
}
