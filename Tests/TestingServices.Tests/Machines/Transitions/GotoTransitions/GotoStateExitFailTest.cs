// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

        private class Program : Machine
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
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }

            private class Done : MachineState
            {
            }
        }

        [Fact]
        public void TestGotoStateExitFail()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(Program));
            });

            this.AssertFailed(test, 1, true);
        }
    }
}
