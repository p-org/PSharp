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

        class Client : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Init : MachineState { }

            void Initialize()
            {
                // Start a timer, and then stop it immediately.
                var timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                this.StopTimer(timer);
            }

            void HandleTimeout()
            {
                // Timeout in the interval between starting and disposing the timer.
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
        public void TestStartStopTimer()
        {
            var configuration = base.GetConfiguration();
            configuration.LivenessTemperatureThreshold = 150;
            configuration.MaxSchedulingSteps = 300;
            configuration.SchedulingIterations = 1000;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Client));
            });

            base.AssertFailed(configuration, test,
                "Monitor 'LivenessMonitor' detected liveness bug in hot state " +
                "'LivenessMonitor.NoTimeoutReceived' at the end of program execution.",
                true);
        }
    }
}
