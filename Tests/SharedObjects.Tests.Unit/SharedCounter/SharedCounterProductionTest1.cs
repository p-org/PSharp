// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedCounterProductionTest1 : BaseTest
    {
        const int N = 100000;

        class E : Event
        {
            public ISharedCounter counter;
            public TaskCompletionSource<bool> tcs;

            public E(ISharedCounter counter, TaskCompletionSource<bool> tcs)
            {
                this.counter = counter;
                this.tcs = tcs;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = (this.ReceivedEvent as E).counter;
                var tcs = (this.ReceivedEvent as E).tcs;

                for(int i = 0; i < N; i++)
                {
                    counter.Increment();

                    var v1 = counter.GetValue();
                    this.Assert(v1 == 1 || v1 == 2);

                    counter.Decrement();

                    var v2 = counter.GetValue();
                    this.Assert(v2 == 0 || v2 == 1);

                    counter.Add(1);

                    var v3 = counter.GetValue();
                    this.Assert(v3 == 1 || v3 == 2);

                    counter.Add(-1);

                    var v4 = counter.GetValue();
                    this.Assert(v4 == 0 || v4 == 1);
                }

                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestCounter()
        {
            var runtime = PSharpRuntime.Create();
            var counter = SharedCounter.Create(runtime, 0);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += delegate
            {
                failed = true;
                tcs1.SetResult(true);
                tcs2.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M), new E(counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(M), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
        }
    }
}
