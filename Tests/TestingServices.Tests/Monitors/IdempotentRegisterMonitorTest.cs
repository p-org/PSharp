// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class IdempotentRegisterMonitorTest : BaseTest
    {
        public IdempotentRegisterMonitorTest(ITestOutputHelper output)
            : base(output)
        { }

        class Counter
        {
            public int Value;

            public Counter()
            {
                this.Value = 0;
            }
        }

        class E : Event
        {
            public Counter Counter;

            public E(Counter counter)
            {
                this.Counter = counter;
            }
        }

        class M : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            class Init : MonitorState { }

            void Check()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                counter.Value++;
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                Counter counter = new Counter();
                this.Monitor(typeof(M), new E(counter));
                this.Assert(counter.Value == 1, "Monitor created more than once.");
            }
        }

        [Fact]
        public void TestIdempotentRegisterMonitorInvocation()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(M));
                r.RegisterMonitor(typeof(M));
                MachineId n = r.CreateMachine(typeof(N));
            });

            base.AssertSucceeded(test);
        }
    }
}
