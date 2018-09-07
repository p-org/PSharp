// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryMockTest6 : BaseTest
    {
        class E : Event
        {
            public ISharedDictionary<int, string> counter;

            public E(ISharedDictionary<int, string> counter)
            {
                this.counter = counter;
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

                this.CreateMachine(typeof(N), new E(counter));
                counter.TryAdd(1, "M");

                string v;
                var b = counter.TryGetValue(2, out v);

                this.Assert(!b);
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
                counter.TryAdd(2, "N");
            }
        }

        [Fact]
        public void TestDictionaryException()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<IPSharpRuntime>((r) => {
                var counter = SharedDictionary.Create<int, string>(r);
                r.CreateMachine(typeof(M), new E(counter));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }

    }
}
