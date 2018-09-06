// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedCounterMockTest3 : BaseTest
    {
        class E : Event
        {
            public ISharedCounter counter;

            public E(ISharedCounter counter)
            {
                this.counter = counter;
            }
        }

        class Done : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                var n = this.CreateMachine(typeof(N), new E(counter));

                counter.Add(4);
                counter.Increment();
                counter.Add(5);

                this.Send(n, new Done());
            }


        }

        class N : Machine
        {
            ISharedCounter counter;

            [Start]
            [OnEventDoAction(typeof(Done), nameof(Check))]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                counter = (this.ReceivedEvent as E).counter;

                counter.Add(-4);
                counter.Decrement();
                var v = counter.Exchange(100);
                counter.Add(-5);
                counter.Add(v - 100);
            }

            void Check()
            {
                var v = counter.GetValue();
                this.Assert(v == 0);
            }
        }

        [Fact]
        public void TestCounter()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertSucceeded(config, test);
        }
    }
}
