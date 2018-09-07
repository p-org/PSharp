// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class OnExceptionTest
    {
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
                await Task.FromResult(true);
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
                await Task.FromResult(true);
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
                if(ex is UnhandledEventException)
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
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                Assert.True(false);
                failed = true;
                tcs.SetResult(true);
            };

            var e = new E(tcs);
            var m = runtime.CreateMachine(typeof(M1a), e);
            runtime.SendEvent(m, new F());

            tcs.Task.Wait();
            Assert.False(failed);
            Assert.True(e.x == 1);
        }

        [Fact]
        public void TestOnExceptionCalledOnce2()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            var e = new E(tcs);
            runtime.CreateMachine(typeof(M1b), e);

            tcs.Task.Wait(5000); // timeout so the test doesn't deadlock on failure
            Assert.True(failed);
            Assert.True(e.x == 1);
        }

        [Fact]
        public void TestOnExceptionCalledOnceAsync1()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                Assert.True(false);
                failed = true;
                tcs.SetResult(true);
            };

            var e = new E(tcs);
            var m = runtime.CreateMachine(typeof(M2a), e);
            runtime.SendEvent(m, new F());

            tcs.Task.Wait();
            Assert.False(failed);
            Assert.True(e.x == 1);
        }

        [Fact]
        public void TestOnExceptionCalledOnceAsync2()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            var e = new E(tcs);
            runtime.CreateMachine(typeof(M2b), e);

            tcs.Task.Wait(5000); // timeout so the test doesn't deadlock on failure
            Assert.True(failed);
            Assert.True(e.x == 1);
        }

        [Fact]
        public void TestOnExceptionCanHalt()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.TrySetResult(false);
            };

            var e = new E(tcs);
            runtime.CreateMachine(typeof(M3), e);

            tcs.Task.Wait();
            Assert.False(failed);
            Assert.True(tcs.Task.Result);
        }

        [Fact]
        public void TestUnHandledEventCanHalt()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.TrySetResult(false);
            };

            var e = new E(tcs);
            var m = runtime.CreateMachine(typeof(M4), e);
            runtime.SendEvent(m, new F());

            tcs.Task.Wait();
            Assert.False(failed);
            Assert.True(tcs.Task.Result);
        }

    }
}
