//-----------------------------------------------------------------------
// <copyright file="OnExceptionTest.cs">
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

using System;
using System.Threading.Tasks;
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
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
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
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await Task.Yield();
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();                
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
                await Task.Yield();
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
                if(ex is UnHandledEventException)
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
                failed = true;
                tcs.SetResult(true);
            };

            var e = new E(tcs);
            runtime.CreateMachine(typeof(M1a), e);

            tcs.Task.Wait(100);
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

            tcs.Task.Wait(100);
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
                failed = true;
                tcs.SetResult(true);
            };

            var e = new E(tcs);
            runtime.CreateMachine(typeof(M2a), e);

            tcs.Task.Wait(100);
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

            tcs.Task.Wait(1000);
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

            tcs.Task.Wait(1000);
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

            tcs.Task.Wait(1000);
            Assert.False(failed);
            Assert.True(tcs.Task.Result);
        }

    }
}
