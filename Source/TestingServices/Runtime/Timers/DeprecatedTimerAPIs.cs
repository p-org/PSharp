// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Deprecated.Timers;

namespace Microsoft.PSharp.TestingServices.Deprecated.Timers
{
    /// <summary>
    /// Signals next timeout period
    /// </summary>
    internal class RepeatTimeoutEvent : Event { }

    /// <summary>
    /// A timer model, used for testing purposes.
    /// </summary>
    [Obsolete("The ModelTimerMachine class is deprecated; use the new StartTimer/StartPeriodicTimer APIs in the Machine class instead.")]
    public class ModelTimerMachine : Machine
    {
        /// <summary>
        /// Adjust the probability of firing a timeout event.
        /// </summary>
        public static int NumStepsToSkip = 1;

        /// <summary>
        /// Machine to which eTimeout events are dispatched.
        /// </summary>
        private MachineId client;

        /// <summary>
        /// True if periodic eTimeout events are desired.
        /// </summary>
        private bool IsPeriodic;

        /// <summary>
        /// TimerId
        /// </summary>
        private TimerId tid;

        [Start]
        [OnEntry(nameof(InitializeTimer))]
        [OnEventDoAction(typeof(HaltTimerEvent), nameof(DisposeTimer))]
        [OnEventDoAction(typeof(RepeatTimeoutEvent), nameof(SendTimeout))]
        private class Init : MachineState { }

        private void InitializeTimer()
        {
            var e = (this.ReceivedEvent as InitTimerEvent);
            this.client = e.client;
            this.IsPeriodic = e.IsPeriodic;
            this.tid = e.tid;
            this.Send(this.Id, new RepeatTimeoutEvent());
        }

        private void SendTimeout()
        {
            this.Assert(NumStepsToSkip >= 0);

            // If not periodic, send a single timeout event
            if (!this.IsPeriodic)
            {
                // Probability of firing timeout is atmost 1/N
                if ((this.RandomInteger(NumStepsToSkip) == 0) && this.FairRandom())
                {
                    this.Send(this.client, new TimerElapsedEvent(tid));
                }
                else
                {
                    this.Send(this.Id, new RepeatTimeoutEvent());
                }
            }
            else
            {
                // Probability of firing timeout is atmost 1/N
                if ((this.RandomInteger(NumStepsToSkip) == 0) && this.FairRandom())
                {
                    this.Send(this.client, new TimerElapsedEvent(tid));
                }

                this.Send(this.Id, new RepeatTimeoutEvent());
            }
        }

        private void DisposeTimer()
        {
            HaltTimerEvent e = (this.ReceivedEvent as HaltTimerEvent);

            // The client attempting to stop this timer must be the one who created it.
            this.Assert(e.client == this.client);

            // If the client wants to flush the inbox, send a markup event.
            // This marks the endpoint of all timeout events sent by this machine.
            if (e.flush)
            {
                this.Send(this.client, new TimeoutFlushEvent());
            }

            // Stop this machine
            this.Raise(new Halt());
        }
    }
}
