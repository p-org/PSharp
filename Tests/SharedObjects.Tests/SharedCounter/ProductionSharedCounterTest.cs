﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.SharedObjects.Tests
{
    public class ProductionSharedCounterTest : BaseTest
    {
        public ProductionSharedCounterTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public ISharedCounter Counter;
            public TaskCompletionSource<bool> Tcs;

            public E(ISharedCounter counter, TaskCompletionSource<bool> tcs)
            {
                this.Counter = counter;
                this.Tcs = tcs;
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                var tcs = (this.ReceivedEvent as E).Tcs;

                for (int i = 0; i < 100000; i++)
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

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                var tcs = (this.ReceivedEvent as E).Tcs;

                for (int i = 0; i < 1000000; i++)
                {
                    int v;

                    do
                    {
                        v = counter.GetValue();

                    } while (v != counter.CompareExchange(v + 5, v));

                    counter.Add(15);
                    counter.Add(-10);
                }

                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestProductionSharedCounter1()
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

            var m1 = runtime.CreateMachine(typeof(M1), new E(counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(M1), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
        }

        [Fact]
        public void TestProductionSharedCounter2()
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

            var m1 = runtime.CreateMachine(typeof(M2), new E(counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(M2), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
            Assert.True(counter.GetValue() == 1000000 * 20);
        }
    }
}
