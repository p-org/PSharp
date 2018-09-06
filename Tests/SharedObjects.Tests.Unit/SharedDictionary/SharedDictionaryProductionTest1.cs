// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryProductionTest1 : BaseTest
    {
        class E : Event
        {
            public ISharedDictionary<int, string> counter;
            public TaskCompletionSource<bool> tcs;

            public E(ISharedDictionary<int, string> counter, TaskCompletionSource<bool> tcs)
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
                var n = this.CreateMachine(typeof(N), this.ReceivedEvent);

                string v;
                while (counter.TryRemove(1, out v) == false) { }
                this.Assert(v == "N");

                tcs.SetResult(true);
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = (this.ReceivedEvent as E).counter;

                var b = counter.TryAdd(1, "N");
                this.Assert(b == true);
            }
        }

        [Fact]
        public void TestCounter()
        {
            var runtime = PSharpRuntime.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += delegate
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }
    }
}
