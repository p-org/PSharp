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
    public class ExceptionPropagationTest : BaseTest
    {
        public ExceptionPropagationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
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

        private class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
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
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcsFail = new TaskCompletionSource<bool>();
                int count = 0;

                r.OnFailure += (exception) =>
                {
                    if (!(exception is MachineActionExceptionFilterException))
                    {
                        count++;
                        tcsFail.SetException(exception);
                    }
                };

                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M), new Configure(tcs));
                tcs.Task.Wait();
                Task.Delay(10).Wait(); // give it some time

                AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
                Assert.IsType<AssertionFailureException>(ex.InnerException);
                Assert.Equal(1, count);
            });

            this.Run(config, test);
        }

        [Fact]
        public void TestUnhandledExceptionEventHandler()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcsFail = new TaskCompletionSource<bool>();
                int count = 0;
                bool sawFilterException = false;

                r.OnFailure += (exception) =>
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
                r.CreateMachine(typeof(N), new Configure(tcs));
                tcs.Task.Wait();
                Task.Delay(10).Wait(); // give it some time

                AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
                Assert.IsType<AssertionFailureException>(ex.InnerException);
                Assert.IsType<InvalidOperationException>(ex.InnerException.InnerException);
                Assert.Equal(1, count);
                Assert.True(sawFilterException);
            });

            this.Run(config, test);
        }
    }
}
