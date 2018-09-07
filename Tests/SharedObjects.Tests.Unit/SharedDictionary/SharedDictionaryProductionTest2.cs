// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryProductionTest2 : BaseTest
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
                var n = this.CreateMachine(typeof(N), this.ReceivedEvent);
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
                var tcs = (this.ReceivedEvent as E).tcs;

                counter.TryAdd(1, "N");
                var v = counter[2]; // key doesn't exist
                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestDictionaryException()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
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
            Assert.True(failed);
        }
    }
}
