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
    public class OnExceptionTest : BaseTest
    {
        public OnExceptionTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public int x;
            public TaskCompletionSource<bool> tcs;

            public E(TaskCompletionSource<bool> tcs)
            {
                x = 0;
                this.tcs = tcs;
            }
        }

        class F : Event { }

        class M1a : Machine
        {
            E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            void OnF()
            {
                e.tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                e.x++;
                return OnExceptionOutcome.HandledException;
            }
        }

        class M1b : Machine
        {
            E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                e.x++;
                return OnExceptionOutcome.ThrowException;
            }

        }

        class M2a : Machine
        {
            E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await Task.CompletedTask;
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();                
            }

            void OnF()
            {
                e.tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                e.x++;
                return OnExceptionOutcome.HandledException;
            }
        }

        class M2b : Machine
        {
            E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await Task.CompletedTask;
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                e.x++;
                return OnExceptionOutcome.ThrowException;
            }

        }

        class M3 : Machine
        {
            E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
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
                e.tcs.TrySetResult(true);
            }
        }

        class M4 : Machine
        {
            E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
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
                e.tcs.TrySetResult(true);
            }
        }


        [Fact]
        public void TestOnExceptionCalledOnce1()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
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
                Assert.True(e.x == 1);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCalledOnce2()
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

                var e = new E(tcs);
                r.CreateMachine(typeof(M1b), e);

                tcs.Task.Wait(5000); // timeout so the test doesn't deadlock on failure
                Assert.True(failed);
                Assert.True(e.x == 1);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCalledOnceAsync1()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
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
                Assert.True(e.x == 1);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCalledOnceAsync2()
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

                var e = new E(tcs);
                r.CreateMachine(typeof(M2b), e);

                tcs.Task.Wait(5000); // timeout so the test doesn't deadlock on failure
                Assert.True(failed);
                Assert.True(e.x == 1);
            });

            base.Run(config, test);
        }

        [Fact]
        public void TestOnExceptionCanHalt()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
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

            base.Run(config, test);
        }

        [Fact]
        public void TestUnHandledEventCanHalt()
        {
            var config = base.GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) => {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += delegate
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

            base.Run(config, test);
        }

    }
}
