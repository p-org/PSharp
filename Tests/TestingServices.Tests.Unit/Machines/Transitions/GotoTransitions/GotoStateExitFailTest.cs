// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GotoStateExitFailTest : BaseTest
    {
        public GotoStateExitFailTest(ITestOutputHelper output)
            : base(output)
        { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Goto<Done>();
            }

            void ExitInit()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }

            class Done : MachineState { }
        }

        [Fact]
        public void TestGotoStateExitFail()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertFailed(test, 1, true);
        }
    }
}
