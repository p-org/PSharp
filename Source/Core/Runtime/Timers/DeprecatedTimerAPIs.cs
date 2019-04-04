// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace Microsoft.PSharp.Deprecated.Timers
{
#pragma warning disable SA1402 // FileMayOnlyContainASingleType
    internal class InitTimerEvent : Event
    {
        /// <summary>
        /// Id of the machine creating the timer.
        /// </summary>
        public MachineId Client;

        /// <summary>
        /// True if periodic timeout events are desired.
        /// </summary>
        public bool IsPeriodic;

        /// <summary>
        /// Period
        /// </summary>
        public int Period;

        /// <summary>
        /// TimerId
        /// </summary>
        public TimerId Tid;

        public InitTimerEvent(MachineId client, TimerId tid, bool isPeriodic, int period)
        {
            this.Client = client;
            this.IsPeriodic = isPeriodic;
            this.Period = period;
            this.Tid = tid;
        }
    }

    /// <summary>
    /// Event used to flush the queue of a machine of eTimeout events.
    /// A single TimeoutFlushEvent event is dispatched to the queue. Then all eTimeout events are removed until we see the TimeoutFlushEvent event.
    /// </summary>
    internal class TimeoutFlushEvent : Event
    {
    }

    /// <summary>
    /// Event requesting stoppage of timer.
    /// </summary>
    internal class HaltTimerEvent : Event
    {
        /// <summary>
        /// Id of machine invoking the request to stop the timer.
        /// </summary>
        public MachineId Client;

        /// <summary>
        /// True if the user wants to flush the client's inbox of the relevant timeout messages.
        /// </summary>
        public bool Flush;

        /// <summary>
        /// Initializes a new instance of the <see cref="HaltTimerEvent"/> class.
        /// </summary>
        /// <param name="client">Id of machine invoking the request to stop the timer. </param>
        /// <param name="flush">True if the user wants to flush the inbox of relevant timeout messages.</param>
        public HaltTimerEvent(MachineId client, bool flush)
        {
            this.Client = client;
            this.Flush = flush;
        }
    }

    /// <summary>
    /// Timeout event sent by the timer.
    /// </summary>
    public class TimerElapsedEvent : Event
    {
        /// <summary>
        /// TimerId
        /// </summary>
        public readonly TimerId Tid;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerElapsedEvent"/> class.
        /// </summary>
        /// <param name="tid">Tid</param>
        public TimerElapsedEvent(TimerId tid)
        {
            this.Tid = tid;
        }
    }

    /// <summary>
    /// Unique identifier for a timer
    /// </summary>
    public class TimerId
    {
        /// <summary>
        /// The timer machine id
        /// </summary>
        internal readonly MachineId Mid;

        /// <summary>
        /// Payload
        /// </summary>
        public readonly object Payload;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerId"/> class.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="payload">Payload</param>
        internal TimerId(MachineId mid, object payload)
        {
            this.Mid = mid;
            this.Payload = payload;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is TimerId tid)
            {
                return this.Mid == tid.Mid;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Mid.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current timer id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return string.Format("Timer[{0},{1}]", this.Mid, this.Payload != null ? this.Payload.ToString() : "null");
        }
    }

    /// <summary>
    /// Extends the P# Machine with a simple timer.
    /// </summary>
    [Obsolete("The TimedMachine class is deprecated; use the new StartTimer/StartPeriodicTimer APIs in the Machine class instead.")]
    public abstract class TimedMachine : Machine
    {
        /// <summary>
        /// The set of currently active timer ids.
        /// </summary>
        private readonly HashSet<TimerId> TimerIds = new HashSet<TimerId>();

        /// <summary>
        /// Start a timer.
        /// </summary>
        /// <param name="payload">Payload of the timeout event.</param>
        /// <param name="period">Periodicity of the timeout events in ms.</param>
        /// <param name="isPeriodic">Specifies whether a periodic timer is desired.</param>
        /// <returns>The id of the created timer.</returns>
        protected TimerId StartTimer(object payload, int period, bool isPeriodic)
        {
            // The specified period must be valid
            this.Assert(period >= 0, "Timer period must be non-negative");

            var mid = this.Runtime.CreateMachineId(this.Runtime.GetTimerMachineType());
            var tid = new TimerId(mid, payload);

            this.Runtime.CreateMachine(mid, this.Runtime.GetTimerMachineType(), new InitTimerEvent(this.Id, tid, isPeriodic, period));

            this.TimerIds.Add(tid);
            return tid;
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        /// <param name="timer">Id of the timer machine which is being stopped.</param>
        /// <param name="flush">Clear the queue of all timeout events generated by "timer".</param>
        protected async Task StopTimer(TimerId timer, bool flush = true)
        {
            // Check if the user is indeed trying to halt a valid timer
            this.Assert(this.TimerIds.Contains(timer), "Illegal timer-id given to StopTimer");
            this.TimerIds.Remove(timer);

            this.Send(timer.Mid, new HaltTimerEvent(this.Id, flush));

            // Flush the buffer: the timer being stopped sends a markup event to the inbox of this machine.
            // Keep dequeuing eTimeout events (with payload being the timer being stopped), until we see the markup event.
            if (flush)
            {
                while (true)
                {
                    var ev = await this.Receive(
                        Tuple.Create(typeof(TimeoutFlushEvent), new Func<Event, bool>(e => true)),
                        Tuple.Create(typeof(TimerElapsedEvent), new Func<Event, bool>(e => (e as TimerElapsedEvent).Tid == timer)));

                    if (ev is TimeoutFlushEvent)
                    {
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Wrapper class for a system timer.
    /// </summary>
    internal class ProductionTimerMachine : Machine
    {
        /// <summary>
        /// Specified if periodic timeout events are desired.
        /// </summary>
        private bool IsPeriodic;

        /// <summary>
        /// Specify the periodicity of timeout events.
        /// </summary>
        private int Period;

        /// <summary>
        /// Machine to which eTimeout events are dispatched.
        /// </summary>
        private MachineId Client;

        /// <summary>
        /// TimerId
        /// </summary>
        private TimerId tid;

        /// <summary>
        /// System timer to generate Elapsed timeout events in production mode.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Flag to prevent timeout events being sent after stopping the timer.
        /// </summary>
        private bool IsTimerEnabled = false;

        /// <summary>
        /// Used to synchronize the Elapsed event handler with timer stoppage.
        /// </summary>
        private readonly object tlock = new object();

        [Start]
        [OnEntry(nameof(InitializeTimer))]
        [OnEventDoAction(typeof(HaltTimerEvent), nameof(DisposeTimer))]
        private class Init : MachineState
        {
        }

        private void InitializeTimer()
        {
            var e = this.ReceivedEvent as InitTimerEvent;
            this.Client = e.Client;
            this.IsPeriodic = e.IsPeriodic;
            this.Period = e.Period;
            this.tid = e.Tid;

            this.IsTimerEnabled = true;
            this.timer = new System.Timers.Timer(this.Period);

            if (!this.IsPeriodic)
            {
                this.timer.AutoReset = false;
            }

            this.timer.Elapsed += this.ElapsedEventHandler;
            this.timer.Start();
        }

        /// <summary>
        /// Handler for the Elapsed event generated by the system timer.
        /// </summary>
        private void ElapsedEventHandler(object source, ElapsedEventArgs e)
        {
            lock (this.tlock)
            {
                if (this.IsTimerEnabled)
                {
                    this.Runtime.SendEvent(this.Client, new TimerElapsedEvent(this.tid));
                }
            }
        }

        private void DisposeTimer()
        {
            HaltTimerEvent e = this.ReceivedEvent as HaltTimerEvent;

            // The client attempting to stop this timer must be the one who created it.
            this.Assert(e.Client == this.Client);

            lock (this.tlock)
            {
                this.IsTimerEnabled = false;
                this.timer.Stop();
                this.timer.Dispose();
            }

            // If the client wants to flush the inbox, send a markup event.
            // This marks the endpoint of all timeout events sent by this machine.
            if (e.Flush)
            {
                this.Send(this.Client, new TimeoutFlushEvent());
            }

            // Stop this machine
            this.Raise(new Halt());
        }
    }
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
}
