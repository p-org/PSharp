// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryMockTest5 : BaseTest
    {
        class E : Event
        {
            public ISharedDictionary<int, string> counter;
            public bool flag;

            public E(ISharedDictionary<int, string> counter, bool flag)
            {
                this.counter = counter;
                this.flag = flag;
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
                var flag = (this.ReceivedEvent as E).flag;

                counter.TryAdd(1, "M");

                if (flag)
                {
                    this.CreateMachine(typeof(N), new E(counter, false));
                }

                string v;
                var b = counter.TryGetValue(2, out v);

                if (!flag)
                {
                    this.Assert(!b);
                }

                if (b)
                {
                    this.Assert(v == "N");
                }
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
                bool b;

                b = counter.TryGetValue(1, out v);
                this.Assert(b);
                this.Assert(v == "M");

                counter.TryAdd(2, "N");
            }
        }

        [Fact]
        public void TestDictionarySuccess1()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<IPSharpRuntime>((r) => {
                var counter = SharedDictionary.Create<int, string>(r);
                r.CreateMachine(typeof(M), new E(counter, true));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestDictionarySuccess2()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<IPSharpRuntime>((r) => {
                var counter = SharedDictionary.Create<int, string>(r);
                r.CreateMachine(typeof(M), new E(counter, false));
            });

            base.AssertSucceeded(config, test);
        }
    }
}
