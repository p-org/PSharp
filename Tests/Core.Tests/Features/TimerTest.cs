// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class TimerTest : BaseTest
    {
        public TimerTest(ITestOutputHelper output)
            : base(output)
        { }

        class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        class TransferTimerEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public TimerInfo Timer;

            public TransferTimerEvent(TaskCompletionSource<bool> Tcs, TimerInfo timer)
            {
                this.Tcs = Tcs;
                this.Timer = timer;
            }
        }

        class T1 : Machine
        {
            TaskCompletionSource<bool> Tcs;

            TimerInfo Timer;
            int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Count = 0;

                // Start a regular timer.
                this.Timer = this.StartTimer(TimeSpan.FromMilliseconds(10));
            }

            void HandleTimeout()
            {
                this.Count++;
                if (this.Count == 1)
                {
                    this.Tcs.SetResult(true);
                    this.Raise(new Halt());
                    return;
                }

                this.Tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }

        class T2 : Machine
        {
            TaskCompletionSource<bool> Tcs;

            TimerInfo Timer;
            int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
            }

            void HandleTimeout()
            {
                this.Count++;
                if (this.Count == 10)
                {
                    this.StopTimer(this.Timer);
                    this.Tcs.SetResult(true);
                    this.Raise(new Halt());
                }
            }
        }

        class T3 : Machine
        {
            TaskCompletionSource<bool> Tcs;

            TimerInfo PingTimer;
            TimerInfo PongTimer;

            /// <summary>
            /// Start the PingTimer and start handling the timeout events from it.
            /// After handling 10 events, stop the timer and move to the Pong state.
            /// </summary>
            [Start]
            [OnEntry(nameof(DoPing))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            class Ping : MachineState { }

            /// <summary>
            /// Start the PongTimer and start handling the timeout events from it.
            /// After handling 10 events, stop the timer and move to the Ping state.
            /// </summary>
            [OnEntry(nameof(DoPong))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Pong : MachineState { }

            private async Task DoPing()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;

                this.PingTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));
                await Task.Delay(100);
                this.StopTimer(this.PingTimer);

                this.Goto<Pong>();
            }

            private void DoPong()
            {
                this.PongTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            }

            private void HandleTimeout()
            {
                var timeout = (this.ReceivedEvent as TimerElapsedEvent);
                if (timeout.Info == this.PongTimer)
                {
                    this.Tcs.SetResult(true);
                    this.Raise(new Halt());
                }
                else
                {
                    this.Tcs.SetResult(false);
                    this.Raise(new Halt());
                }
            }
        }

        class T4 : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }

            void Initialize()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;

                try
                {
                    this.StartTimer(TimeSpan.FromSeconds(-1));
                }
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(ex.Message);
                    tcs.SetResult(true);
                    this.Raise(new Halt());
                    return;
                }

                tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }

        class T5 : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }

            void Initialize()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;

                try
                {
                    this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1));
                }
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(ex.Message);
                    tcs.SetResult(true);
                    this.Raise(new Halt());
                    return;
                }

                tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }

        [Fact]
        public void TestBasicTimerOperation()
        {
            var configuration = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T1), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            base.Run(configuration, test);
        }

        [Fact]
        public void TestBasicPeriodicTimerOperation()
        {
            var configuration = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T2), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            base.Run(configuration, test);
        }

        [Fact]
        public void TestDropTimeoutsAfterTimerDisposal()
        {
            var configuration = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T3), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            base.Run(configuration, test);
        }

        [Fact]
        public void TestIllegalDueTimeSpecification()
        {
            var configuration = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T4), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            base.Run(configuration, test);
        }

        [Fact]
        public void TestIllegalPeriodSpecification()
        {
            var configuration = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T5), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            base.Run(configuration, test);
        }
    }
}
