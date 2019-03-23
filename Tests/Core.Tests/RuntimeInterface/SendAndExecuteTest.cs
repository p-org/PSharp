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
        {
        }

        private class Config1 : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Config1(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class Config2 : Event
        {
            public bool HandleException;
            public TaskCompletionSource<bool> Tcs;

            public Config2(bool handleEx, TaskCompletionSource<bool> tcs)
            {
                this.HandleException = handleEx;
                this.Tcs = tcs;
            }
        }

        private class E1 : Event
        {
            public int Value;

            public E1()
            {
                this.Value = 0;
            }
        }

        private class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
            {
                this.Id = id;
            }
        }

        private class E3 : Event
        {
        }

        private class M_Halts : Event
        {
        }

        private class SE_Returns : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var e = new E1();
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N1));
                await this.Runtime.SendEventAndExecute(m, e);
                this.Assert(e.Value == 1);
                tcs.SetResult(true);
            }
        }

        private class N1 : Machine
        {
            private bool LEHandled = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleEventE))]
            [OnEventDoAction(typeof(E3), nameof(HandleEventLE))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E3());
            }

            private void HandleEventLE()
            {
                this.LEHandled = true;
            }

            private void HandleEventE()
            {
                this.Assert(this.LEHandled);
                var e = this.ReceivedEvent as E1;
                e.Value = 1;
            }
        }

        [Fact]
        public void TestSyncSendBlocks()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateMachine(typeof(M1), new Config1(tcs));
                tcs.Task.Wait(1000);

                Assert.False(failed);
            });

            this.Run(config, test);
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E3))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N2), new E2(this.Id));
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Assert(handled);
                tcs.SetResult(true);
            }
        }

        private class N2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E3))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E2).Id;
                var handled = await this.Id.Runtime.SendEventAndExecute(creator, new E3());
                this.Assert(!handled);
            }
        }

        [Fact]
        public void TestSendCycleDoesNotDeadlock()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateMachine(typeof(M2), new Config1(tcs));
                tcs.Task.Wait();

                Assert.False(failed);
            });

            this.Run(config, test);
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N3));
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }
        }

        private class N3 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E3), nameof(HandleE))]
            private class Init : MachineState
            {
            }

            private void HandleE()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Monitor<SafetyMonitor>(new M_Halts());
            }
        }

        private class SafetyMonitor : Monitor
        {
            private bool MHalted = false;
            private bool SEReturned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(M_Halts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SE_Returns), nameof(OnSEReturns))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Done : MonitorState
            {
            }

            private void OnMHalts()
            {
                this.Assert(this.SEReturned == false);
                this.MHalted = true;
            }

            private void OnSEReturns()
            {
                this.Assert(this.MHalted);
                this.SEReturned = true;
                this.Goto<Done>();
            }
        }

        [Fact]
        public void TestMachineHaltsOnSendExec()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            config.EnableMonitorsInProduction = true;

            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(M3), new Config1(tcs));
                tcs.Task.Wait();

                Assert.False(failed);
            });

            this.Run(config, test);
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
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

        private class N4 : Machine
        {
            private bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(HandleE))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.HandleException = (this.ReceivedEvent as Config2).HandleException;
            }

            private void HandleE()
            {
                throw new Exception();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return this.HandleException ? OnExceptionOutcome.HandledException : OnExceptionOutcome.ThrowException;
            }
        }

        [Fact]
        public void TestHandledExceptionOnSendExec()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateMachine(typeof(M4), new Config2(true, tcs));
                tcs.Task.Wait();

                Assert.False(failed);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestUnHandledExceptionOnSendExec()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                var message = string.Empty;

                r.OnFailure += (ex) =>
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

            this.Run(config, test);
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config1).Tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(N5));
                var handled = await this.Runtime.SendEventAndExecute(m, new E3());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }
        }

        private class N5 : Machine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact]
        public void TestUnhandledEventOnSendExec()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                var message = string.Empty;

                r.OnFailure += (ex) =>
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
                Assert.Equal(
                    "Machine 'Microsoft.PSharp.Core.Tests.SendAndExecuteTest+N5(1)' received event " +
                    "'Microsoft.PSharp.Core.Tests.SendAndExecuteTest+E3' that cannot be handled.", message);
            });

            this.Run(config, test);
        }
    }
}
