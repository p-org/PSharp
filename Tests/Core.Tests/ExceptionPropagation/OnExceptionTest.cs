﻿// ------------------------------------------------------------------------------------------------
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
    public class OnExceptionTest : BaseTest
    {
        public OnExceptionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public int X;
            public TaskCompletionSource<bool> Tcs;

            public E(TaskCompletionSource<bool> tcs)
            {
                this.X = 0;
                this.Tcs = tcs;
            }
        }

        private class F : Event
        {
        }

        private class M1a : Machine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            private void OnF()
            {
                this.e.Tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.HandledException;
            }
        }

        private class M1b : Machine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M2a : Machine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.CompletedTask;
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            private void OnF()
            {
                this.e.Tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.HandledException;
            }
        }

        private class M2b : Machine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.CompletedTask;
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M3 : Machine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HaltMachine;
            }

            protected override void OnHalt()
            {
                this.e.Tcs.TrySetResult(true);
            }
        }

        private class M4 : Machine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.HaltMachine;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override void OnHalt()
            {
                this.e.Tcs.TrySetResult(true);
            }
        }

        [Fact]
        public void TestOnExceptionCalledOnce1()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    Assert.True(false);
                    failed = true;
                    tcs.SetResult(true);
                };

                var e = new E(tcs);
                var m = r.CreateMachine(typeof(M1a), e);
                r.SendEvent(m, new F());

                tcs.Task.Wait();
                Assert.False(failed);
                Assert.True(e.X == 1);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCalledOnce2()
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

                var e = new E(tcs);
                r.CreateMachine(typeof(M1b), e);

                tcs.Task.Wait(5000); // timeout so the test doesn't deadlock on failure
                Assert.True(failed);
                Assert.True(e.X == 1);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCalledOnceAsync1()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    Assert.True(false);
                    failed = true;
                    tcs.SetResult(true);
                };

                var e = new E(tcs);
                var m = r.CreateMachine(typeof(M2a), e);
                r.SendEvent(m, new F());

                tcs.Task.Wait();
                Assert.False(failed);
                Assert.True(e.X == 1);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCalledOnceAsync2()
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

                var e = new E(tcs);
                r.CreateMachine(typeof(M2b), e);

                tcs.Task.Wait(5000); // timeout so the test doesn't deadlock on failure
                Assert.True(failed);
                Assert.True(e.X == 1);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCanHalt()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.TrySetResult(false);
                };

                var e = new E(tcs);
                r.CreateMachine(typeof(M3), e);

                tcs.Task.Wait();
                Assert.False(failed);
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestUnHandledEventCanHalt()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.TrySetResult(false);
                };

                var e = new E(tcs);
                var m = r.CreateMachine(typeof(M4), e);
                r.SendEvent(m, new F());

                tcs.Task.Wait();
                Assert.False(failed);
                Assert.True(tcs.Task.Result);
            });

            this.Run(config, test);
        }
    }
}
