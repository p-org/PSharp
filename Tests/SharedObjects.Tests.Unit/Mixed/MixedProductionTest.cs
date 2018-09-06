﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class MixedProductionTest : BaseTest
    {
        const int T = 100;

        class E : Event
        {
            public ISharedDictionary<int, string> dictionary;
            public ISharedCounter counter;
            public TaskCompletionSource<bool> tcs;

            public E(ISharedDictionary<int, string> dictionary, ISharedCounter counter, TaskCompletionSource<bool> tcs)
            {
                this.dictionary = dictionary;
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
                var dictionary = (this.ReceivedEvent as E).dictionary;
                var counter = (this.ReceivedEvent as E).counter;
                var tcs = (this.ReceivedEvent as E).tcs;

                for (int i = 0; i < T; i++)
                {
                    dictionary.TryAdd(i, i.ToString());
                }

                for (int i = 0; i < T; i++)
                {
                    string v;
                    var b = dictionary.TryRemove(i, out v);
                    this.Assert(b == false || v == i.ToString());

                    if (b)
                    {
                        counter.Increment();
                    }
                }

                var c = dictionary.Count;
                this.Assert(c == 0);
                tcs.TrySetResult(true);
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var dictionary = (this.ReceivedEvent as E).dictionary;
                var counter = (this.ReceivedEvent as E).counter;
                var tcs = (this.ReceivedEvent as E).tcs;

                for (int i = 0; i < T; i++)
                {
                    string v;
                    var b = dictionary.TryRemove(i, out v);
                    this.Assert(b == false || v == i.ToString());

                    if (b)
                    {
                        counter.Increment();
                    }
                }

                tcs.TrySetResult(true);
            }
        }

        [Fact]
        public void TestDictionaryCount()
        {
            var runtime = PSharpRuntime.Create();
            var dictionary = SharedDictionary.Create<int, string>(runtime);
            var counter = SharedCounter.Create(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += delegate
            {
                failed = true;
                tcs1.TrySetResult(true);
                tcs2.TrySetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M), new E(dictionary, counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(N), new E(dictionary, counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
            Assert.True(counter.GetValue() == T);
        }
    }
}
