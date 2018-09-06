// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class ExceptionPropagationTest
    {
        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;
                try
                {
                    this.Assert(false);
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        class N: Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;
                try
                {
                    throw new InvalidOperationException();
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        [Fact]
        public void TestAssertFailureNoEventHandler()
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            tcs.Task.Wait();
        }

        [Fact]
        public void TestAssertFailureEventHandler()
        {
            var tcsFail = new TaskCompletionSource<bool>();
            int count = 0;

            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.OnFailure += delegate (Exception exception)
            {
                if (!(exception is MachineActionExceptionFilterException))
                {
                    count++;
                    tcsFail.SetException(exception);
                }
            };

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            tcs.Task.Wait();
            Task.Delay(10).Wait(); // give it some time

            AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
            Assert.IsType<AssertionFailureException>(ex.InnerException);
            Assert.Equal(1, count);
        }

        [Fact]
        public void TestUnhandledExceptionEventHandler()
        {
            var tcsFail = new TaskCompletionSource<bool>();
            int count = 0;
            bool sawFilterException = false;

            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.OnFailure += delegate (Exception exception)
            {
                // This test throws an exception that we should receive a filter call for
                if (exception is MachineActionExceptionFilterException)
                {
                    sawFilterException = true;
                    return;
                }
                count++;
                tcsFail.SetException(exception);
            };

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(N), new Configure(tcs));
            tcs.Task.Wait();
            Task.Delay(10).Wait(); // give it some time

            AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
            Assert.IsType<AssertionFailureException>(ex.InnerException);
            Assert.IsType<InvalidOperationException>(ex.InnerException.InnerException);
            Assert.Equal(1, count);
            Assert.True(sawFilterException);
        }
    }
}
