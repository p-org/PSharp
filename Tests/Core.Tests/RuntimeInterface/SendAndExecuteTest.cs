// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class SendAndExecuteTest : BaseTest
    {
        public SendAndExecuteTest(ITestOutputHelper output)
            : base(output)
        { }

        class Config1 : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Config1(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        class Config2 : Event
        {
            public bool HandleException;
            public TaskCompletionSource<bool> Tcs;

            public Config2(bool handleEx, TaskCompletionSource<bool> tcs)
            {
                this.HandleException = handleEx;
                this.Tcs = tcs;
            }
        }

        class E1 : Event
        {
            public int Value;

            public E1()
            {
                this.Value = 0;
            }
        }

        class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
            {
                this.Id = id;
            }
        }

        class E3 : Event { }

        class M_Halts : Event { }

        class SE_Returns : Event { }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var e = new E1();
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N1));
                await this.Runtime.SendEventAndExecute(m, e);
                this.Assert(e.Value == 1);
                tcs.SetResult(true);
            }
        }

        class N1 : Machine
        {
            bool LE_Handled = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleEventE))]
            [OnEventDoAction(typeof(E3), nameof(HandleEventLE))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E3());
            }

            void HandleEventLE()
            {
                LE_Handled = true;
            }

            void HandleEventE()
            {
                this.Assert(LE_Handled);
                var e = (this.ReceivedEvent as E1);
                e.Value = 1;
            }
        }


        [Fact]
        public void TestSyncSendBlocks()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateMachine(typeof(M1), new Config1(tcs));
                tcs.Task.Wait(1000);

                Assert.False(failed);
            });

            base.Run(config, test);
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E3))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N2), new E2(this.Id));
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Assert(handled);
                tcs.SetResult(true);
            }
        }

        class N2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E3))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E2).Id;
                var handled = await this.Id.Runtime.SendEventAndExecute(creator, new E3());
                this.Assert(!handled);
            }
        }

        [Fact]
        public void TestSendCycleDoesNotDeadlock()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateMachine(typeof(M2), new Config1(tcs));
                tcs.Task.Wait();

                Assert.False(failed);
            });

            base.Run(config, test);
        }

        class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N3));
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }
        }

        class N3 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E3), nameof(HandleE))]
            class Init : MachineState { }

            void HandleE()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Monitor<SafetyMonitor>(new M_Halts());
            }
        }

        class SafetyMonitor : Monitor
        {
            bool M_halted = false;
            bool SE_returned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(M_Halts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SE_Returns), nameof(OnSEReturns))]
            class Init : MonitorState { }

            [Cold]
            class Done : MonitorState { }

            void OnMHalts()
            {
                this.Assert(SE_returned == false);
                M_halted = true;
            }

            void OnSEReturns()
            {
                this.Assert(M_halted);
                SE_returned = true;
                this.Goto<Done>();
            }
        }

        [Fact]
        public void TestMachineHaltsOnSendExec()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            config.EnableMonitorsInProduction = true;

            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(M3), new Config1(tcs));
                tcs.Task.Wait();

                Assert.False(failed);
            });

            base.Run(config, test);
        }

        class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config2).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N4), this.ReceivedEvent);
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(false);
                return OnExceptionOutcome.ThrowException;
            }
        }

        class N4 : Machine
        {
            bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(HandleE))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.HandleException = (this.ReceivedEvent as Config2).HandleException;
            }

            void HandleE()
            {
                throw new Exception();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return HandleException ? OnExceptionOutcome.HandledException : OnExceptionOutcome.ThrowException;
            }
        }

        [Fact]
        public void TestHandledExceptionOnSendExec()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateMachine(typeof(M4), new Config2(true, tcs));
                tcs.Task.Wait();

                Assert.False(failed);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestUnHandledExceptionOnSendExec()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                var message = string.Empty;

                r.OnFailure += delegate (Exception ex)
                {
                    if (!failed)
                    {
                        message = (ex is MachineActionExceptionFilterException) ? ex.InnerException.Message : ex.Message;
                        failed = true;
                        tcs.TrySetResult(false);
                    }
                };

                r.CreateMachine(typeof(M4), new Config2(false, tcs));
                tcs.Task.Wait();

                Assert.True(failed);
                Assert.StartsWith("Exception of type 'System.Exception' was thrown", message);
            });

            base.Run(config, test);
        }

        class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N5));
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }
        }

        class N5 : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestUnhandledEventOnSendExec()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                var message = string.Empty;

                r.OnFailure += delegate (Exception ex)
                {
                    if (!failed)
                    {
                        message = (ex is MachineActionExceptionFilterException) ? ex.InnerException.Message : ex.Message;
                        failed = true;
                        tcs.TrySetResult(false);
                    }
                };

                r.CreateMachine(typeof(M5), new Config1(tcs));
                tcs.Task.Wait();

                Assert.True(failed);
                Assert.Equal("Machine 'Microsoft.PSharp.Core.Tests.SendAndExecuteTest+N5(1)' received event " +
                    "'Microsoft.PSharp.Core.Tests.SendAndExecuteTest+E3' that cannot be handled.", message);
            });

            base.Run(config, test);
        }
    }
}
