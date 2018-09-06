// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedRegisterProductionTest : BaseTest
    {
        class E : Event
        {
            public ISharedRegister<int> counter;
            public TaskCompletionSource<bool> tcs;

            public E(ISharedRegister<int> counter, TaskCompletionSource<bool> tcs)
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

                for (int i = 0; i < 1000; i++)
                {
                    counter.Update(x => x + 5);

                    var v1 = counter.GetValue();
                    this.Assert(v1 == 10 || v1 == 15);

                    counter.Update(x => x - 5);

                    var v2 = counter.GetValue();
                    this.Assert(v2 == 5 || v2 == 10);
                }

                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestRegister()
        {
            var runtime = PSharpRuntime.Create();
            var counter = SharedRegister.Create<int>(runtime, 0);
            counter.SetValue(5);

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
