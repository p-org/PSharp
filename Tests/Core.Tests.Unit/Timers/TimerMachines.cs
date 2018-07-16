//-----------------------------------------------------------------------
// <copyright file="TimerMachines.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    internal class NonMachineSubClass { }

    internal class Configure : Event
    {
        public TaskCompletionSource<bool> TCS;
        public bool periodic;

        public Configure(TaskCompletionSource<bool> tcs, bool periodic)
        {
            this.TCS = tcs;
            this.periodic = periodic;
        }
    }

    internal class ConfigureWithPeriod : Event
    {
        public TaskCompletionSource<bool> TCS;
        public int period;

        public ConfigureWithPeriod(TaskCompletionSource<bool> tcs, int period)
        {
            this.TCS = tcs;
            this.period = period;
        }
    }

    internal class Marker : Event { }

    internal class TransferTimerAndTCS : Event
    {
        public TimerId tid;
        public TaskCompletionSource<bool> TCS;

        public TransferTimerAndTCS(TimerId tid, TaskCompletionSource<bool> TCS)
        {
            this.tid = tid;
            this.TCS = TCS;
        }
    }

    class T1 : TimedMachine
    {
        TimerId tid;
        object payload = new object();
        TaskCompletionSource<bool> tcs;
        int count;
        bool periodic;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            Configure e = (this.ReceivedEvent as Configure);
            tcs = e.TCS;
            periodic = e.periodic;
            count = 0;

            if (periodic)
            {
                // Start a periodic timer with 10ms timeouts
                tid = StartTimer(payload, 10, true);
            }
            else
            {
                // Start a one-off timer.
                tid = StartTimer(payload, 10, false);
            }
        }

        async Task HandleTimeout()
        {
            count++;

            // for testing single timeout
            if (!periodic)
            {
                // for a single timer, exactly one timeout should be received
                if (count == 1)
                {
                    await StopTimer(tid, true);
                    tcs.SetResult(true);
                    this.Raise(new Halt());
                }
                else
                {
                    await StopTimer(tid, true);
                    tcs.SetResult(false);
                    this.Raise(new Halt());
                }
            }

            // for testing periodic timeouts
            else
            {
                if (count == 100)
                {
                    await StopTimer(tid, true);
                    tcs.SetResult(true);
                    this.Raise(new Halt());
                }
            }
        }
    }

    class FlushingClient : TimedMachine
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

        TaskCompletionSource<bool> tcs;

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
            tcs = (this.ReceivedEvent as Configure).TCS;

            // Start a periodic timer with timeout interval of 1sec.
            // The timer generates TimerElapsedEvent with 'm' as payload.
            pingTimer = StartTimer(payload, 5, true);
            await Task.Delay(100);
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

            if (e.Tid == this.pongTimer)
            {
                tcs.SetResult(true);
                this.Raise(new Halt());
            }
            else
            {
                tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }
    }

    class T2 : TimedMachine
    {
        TimerId tid;
        TaskCompletionSource<bool> tcs;
        object payload = new object();
        MachineId m;

        [Start]
        [OnEntry(nameof(Initialize))]
        [IgnoreEvents(typeof(TimerElapsedEvent))]
        class Init : MachineState { }

        void Initialize()
        {
            tcs = (this.ReceivedEvent as Configure).TCS;
            tid = this.StartTimer(this.payload, 100, true);
            m = CreateMachine(typeof(T3), new TransferTimerAndTCS(tid, tcs));
            this.Raise(new Halt());
        }
    }

    class T3 : TimedMachine
    {
        [Start]
        [OnEntry(nameof(Initialize))]
        class Init : MachineState { }

        async Task Initialize()
        {
            TimerId tid = (this.ReceivedEvent as TransferTimerAndTCS).tid;
            TaskCompletionSource<bool> tcs = (this.ReceivedEvent as TransferTimerAndTCS).TCS;

            // trying to stop a timer created by a different machine. 
            // should throw an assertion violation
            try
            {
                await this.StopTimer(tid, true);
            }
            catch (AssertionFailureException)
            {
                tcs.SetResult(true);
                this.Raise(new Halt());
            }
        }
    }

    class T4 : TimedMachine
    {
        object payload = new object();

        [Start]
        [OnEntry(nameof(Initialize))]
        class Init : MachineState { }

        void Initialize()
        {
            var tcs = (this.ReceivedEvent as ConfigureWithPeriod).TCS;
            var period = (this.ReceivedEvent as ConfigureWithPeriod).period;

            try
            {
                this.StartTimer(this.payload, period, true);
            }
            catch (AssertionFailureException)
            {
                tcs.SetResult(true);
                this.Raise(new Halt());
            }
        }
    }
}
