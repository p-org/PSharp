// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------


using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Deprecated.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Deprecated
{
    public class DeprecatedTimerTest : BaseTest
    {
        public DeprecatedTimerTest(ITestOutputHelper output)
            : base(output)
        { }

        class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;
            public bool periodic;

            public Configure(TaskCompletionSource<bool> tcs, bool periodic)
            {
                this.TCS = tcs;
                this.periodic = periodic;
            }
        }

        class ConfigureWithPeriod : Event
        {
            public TaskCompletionSource<bool> TCS;
            public int period;

            public ConfigureWithPeriod(TaskCompletionSource<bool> tcs, int period)
            {
                this.TCS = tcs;
                this.period = period;
            }
        }

        class Marker : Event { }

        class TransferTimerAndTCS : Event
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
            readonly object payload = new object();
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
                    // Start a one-off timer 
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
            readonly object payload = new object();

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
                var e = (this.ReceivedEvent as TimerElapsedEvent);

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
            readonly object payload = new object();
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
            readonly object payload = new object();

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

        /// <summary>
        /// Check basic functions of a periodic timer.
        /// </summary>
        [Fact]
        public void BasicPeriodicTimerOperationTest()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T1), new Configure(tcs, true));
                Assert.True(tcs.Task.Result);
            });

            base.Run(config, test);
        }

        [Fact]
        public void BasicSingleTimerOperationTest()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T1), new Configure(tcs, false));
                Assert.True(tcs.Task.Result);
            });

            base.Run(config, test);
        }

        /// <summary>
        /// Test if the flushing operation works correctly.
        /// </summary>
        [Fact]
        public void InboxFlushOperationTest()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(FlushingClient), new Configure(tcs, true));
                Assert.True(tcs.Task.Result);
            });

            base.Run(config, test);
        }

        [Fact]
        public void IllegalTimerStoppageTest()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T2), new Configure(tcs, true));
                Assert.True(tcs.Task.Result);
            });

            base.Run(config, test);
        }

        [Fact]
        public void IllegalPeriodSpecificationTest()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T4), new ConfigureWithPeriod(tcs, -1));
                Assert.True(tcs.Task.Result);
            });

            base.Run(config, test);
        }
    }
}
