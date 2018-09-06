// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryMockTest4 : BaseTest
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
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);
                this.CreateMachine(typeof(N), new E(counter));

                counter.TryAdd(1, "M");

                string v;
                var b = counter.TryRemove(1, out v);

                this.Assert(b == false || v == "M");
                this.Assert(counter.Count == 0);
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

                string v;
                var b = counter.TryRemove(1, out v);
                this.Assert(b == false || v == "M");
            }
        }

        [Fact]
        public void TestDictionaryRemove()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertSucceeded(config, test);
        }
    }
}
