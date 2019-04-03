// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Deprecated.Timers;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Deprecated
{
    public class DeprecatedTimerTest : BaseTest
    {
        public DeprecatedTimerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;
            public bool Periodic;

            public Configure(TaskCompletionSource<bool> tcs, bool periodic)
            {
                this.TCS = tcs;
                this.Periodic = periodic;
            }
        }

        private class ConfigureWithPeriod : Event
        {
            public TaskCompletionSource<bool> TCS;
            public int Period;

            public ConfigureWithPeriod(TaskCompletionSource<bool> tcs, int period)
            {
                this.TCS = tcs;
                this.Period = period;
            }
        }

        private class Marker : Event
        {
        }

        private class TransferTimerAndTCS : Event
        {
            public TimerId Tid;
            public TaskCompletionSource<bool> Tcs;

            public TransferTimerAndTCS(TimerId tid, TaskCompletionSource<bool> tcs)
            {
                this.Tid = tid;
                this.Tcs = tcs;
            }
        }

        private class T1 : TimedMachine
        {
            private TimerId tid;
            private readonly object payload = new object();
            private TaskCompletionSource<bool> tcs;
            private int count;
            private bool periodic;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                Configure e = this.ReceivedEvent as Configure;
                this.tcs = e.TCS;
                this.periodic = e.Periodic;
                this.count = 0;

                if (this.periodic)
                {
                    // Start a periodic timer with 10ms timeouts
                    this.tid = this.StartTimer(this.payload, 10, true);
                }
                else
                {
                    // Start a one-off timer
                    this.tid = this.StartTimer(this.payload, 10, false);
                }
            }

            private async Task HandleTimeout()
            {
                this.count++;

                // for testing single timeout
                if (!this.periodic)
                {
                    // for a single timer, exactly one timeout should be received
                    if (this.count == 1)
                    {
                        await this.StopTimer(this.tid, true);
                        this.tcs.SetResult(true);
                        this.Raise(new Halt());
                    }
                    else
                    {
                        await this.StopTimer(this.tid, true);
                        this.tcs.SetResult(false);
                        this.Raise(new Halt());
                    }
                }

                // for testing periodic timeouts
                else
                {
                    if (this.count == 100)
                    {
                        await this.StopTimer(this.tid, true);
                        this.tcs.SetResult(true);
                        this.Raise(new Halt());
                    }
                }
            }
        }

        private class FlushingClient : TimedMachine
        {
            /// <summary>
            /// A dummy payload object received with timeout events.
            /// </summary>
            private readonly object payload = new object();

            /// <summary>
            /// Timer used in the Ping State.
            /// </summary>
            private TimerId pingTimer;

            /// <summary>
            /// Timer used in the Pong state.
            /// </summary>
            private TimerId pongTimer;

            private TaskCompletionSource<bool> tcs;

            /// <summary>
            /// Start the pingTimer and start handling the timeout events from it.
            /// After handling 10 events, stop pingTimer and move to the Pong state.
            /// </summary>
            [Start]
            [OnEntry(nameof(DoPing))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Ping : MachineState
            {
            }

            /// <summary>
            /// Start the pongTimer and start handling the timeout events from it.
            /// After handling 10 events, stop pongTimer and move to the Ping state.
            /// </summary>
            [OnEntry(nameof(DoPong))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeoutForPong))]
            private class Pong : MachineState
            {
            }

            private async Task DoPing()
            {
                this.tcs = (this.ReceivedEvent as Configure).TCS;

                // Start a periodic timer with timeout interval of 1sec.
                // The timer generates TimerElapsedEvent with 'm' as payload.
                this.pingTimer = this.StartTimer(this.payload, 5, true);
                await Task.Delay(100);
                await this.StopTimer(this.pingTimer, flush: true);
                this.Goto<Pong>();
            }

            /// <summary>
            /// Handle timeout events from the pongTimer.
            /// </summary>
            private void DoPong()
            {
                // Start a periodic timer with timeout interval of 0.5sec.
                // The timer generates TimerElapsedEvent with 'm' as payload.
                this.pongTimer = this.StartTimer(this.payload, 50, false);
            }

            private void HandleTimeoutForPong()
            {
                var e = this.ReceivedEvent as TimerElapsedEvent;

                if (e.Tid == this.pongTimer)
                {
                    this.tcs.SetResult(true);
                    this.Raise(new Halt());
                }
                else
                {
                    this.tcs.SetResult(false);
                    this.Raise(new Halt());
                }
            }
        }

        private class T2 : TimedMachine
        {
            private TimerId tid;
            private TaskCompletionSource<bool> tcs;
            private readonly object payload = new object();
            private MachineId m;

            [Start]
            [OnEntry(nameof(Initialize))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                this.tcs = (this.ReceivedEvent as Configure).TCS;
                this.tid = this.StartTimer(this.payload, 100, true);
                this.m = this.CreateMachine(typeof(T3), new TransferTimerAndTCS(this.tid, this.tcs));
                this.Raise(new Halt());
            }
        }

        private class T3 : TimedMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private async Task Initialize()
            {
                TimerId tid = (this.ReceivedEvent as TransferTimerAndTCS).Tid;
                TaskCompletionSource<bool> tcs = (this.ReceivedEvent as TransferTimerAndTCS).Tcs;

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

        private class T4 : TimedMachine
        {
            private readonly object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var tcs = (this.ReceivedEvent as ConfigureWithPeriod).TCS;
                var period = (this.ReceivedEvent as ConfigureWithPeriod).Period;

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
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T1), new Configure(tcs, true));
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }

        [Fact]
        public void BasicSingleTimerOperationTest()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T1), new Configure(tcs, false));
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }

        /// <summary>
        /// Test if the flushing operation works correctly.
        /// </summary>
        [Fact]
        public void InboxFlushOperationTest()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(FlushingClient), new Configure(tcs, true));
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }

        [Fact]
        public void IllegalTimerStoppageTest()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T2), new Configure(tcs, true));
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }

        [Fact]
        public void IllegalPeriodSpecificationTest()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T4), new ConfigureWithPeriod(tcs, -1));
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }
    }
}
