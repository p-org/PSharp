﻿// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class GotoStateExitFailTest : BaseTest
    {
        public GotoStateExitFailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Done>();
            }

            private void ExitInit()
            {
                // This assertion is reachable.
                this.Assert(false, "Bug found.");
            }

            private class Done : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateExitFail()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M));
            },
            expectedError: "Bug found.",
            replay: true);
        }
    }
}
