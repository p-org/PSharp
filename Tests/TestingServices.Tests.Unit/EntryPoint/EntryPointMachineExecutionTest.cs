// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class EntryPointMachineExecutionTest : BaseTest
    {
        class M : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact]
        public void TestEntryPointMachineExecution()
        {
            var test = new Action<PSharpRuntime>((r) => {
                MachineId m = r.CreateMachine(typeof(M));
                MachineId n = r.CreateMachine(typeof(N));
            });

            var bugReport = "Reached test assertion.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
