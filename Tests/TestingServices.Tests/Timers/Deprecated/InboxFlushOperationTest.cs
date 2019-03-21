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
    public class DeprecatedInboxFlushOperationTest : BaseTest
    {
        public DeprecatedInboxFlushOperationTest(ITestOutputHelper output)
            : base(output)
        { }

        private class FlushingClient : TimedMachine
        {
            /// <summary>
            /// A dummy payload object received with timeout events.
            /// </summary>
            object payload = new object();

            /// <summary>
            /// Timer used in the Ping State.
            /// </summary>
            TimerId pingTimer;

            /// <summary>
            /// Timer used in the Pong state.
            /// </summary>
            TimerId pongTimer;

            /// <summary>
            /// Start the pingTimer and start handling the timeout events from it.
            /// After handling 10 events, stop pingTimer and move to the Pong state.
            /// </summary>
            [Start]
            [OnEntry(nameof(DoPing))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            class Ping : MachineState { }

            /// <summary>
            /// Start the pongTimer and start handling the timeout events from it.
            /// After handling 10 events, stop pongTimer and move to the Ping state.
            /// </summary>
            [OnEntry(nameof(DoPong))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeoutForPong))]
            class Pong : MachineState { }

            private async Task DoPing()
            {
                // Start a periodic timer with timeout interval of 1sec.
                // The timer generates TimerElapsedEvent with 'm' as payload.
                pingTimer = StartTimer(payload, 5, true);
                await this.StopTimer(pingTimer, flush: true);
                this.Goto<Pong>();
            }

            /// <summary>
            /// Handle timeout events from the pongTimer.
            /// </summary>
            private void DoPong()
            {
                // Start a periodic timer with timeout interval of 0.5sec.
                // The timer generates TimerElapsedEvent with 'm' as payload.
                pongTimer = StartTimer(payload, 50, false);
            }

            private void HandleTimeoutForPong()
            {
                TimerElapsedEvent e = (this.ReceivedEvent as TimerElapsedEvent);
                this.Assert(e.Tid == this.pongTimer);
            }
        }

        [Fact]
        public void InboxFlushTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            config.MaxSchedulingSteps = 200;
            config.SchedulingStrategy = Utilities.SchedulingStrategy.Portfolio;
            config.RunAsParallelBugFindingTask = true;
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(FlushingClient));
            });
            base.AssertSucceeded(test);
        }
    }
}
