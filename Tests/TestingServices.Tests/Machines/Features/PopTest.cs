﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class PopTest : BaseTest
    {
        public PopTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(Init))]
            public class S1 : MachineState
            {
            }

            private void Init()
            {
                this.Pop();
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEntry(nameof(Init))]
            [OnExit(nameof(Exit))]
            public class S1 : MachineState
            {
            }

            private void Init()
            {
                this.Goto<S2>();
            }

            private void Exit()
            {
                this.Pop();
            }

            public class S2 : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestUnbalancedPop()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M), "M");
            },
            expectedError: "Machine 'M()' popped with no matching push.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestPopDuringOnExit()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(N), "N");
            },
            expectedError: "Machine 'N()' has called raise, goto, push or pop inside an OnExit method.",
            replay: true);
        }
    }
}
