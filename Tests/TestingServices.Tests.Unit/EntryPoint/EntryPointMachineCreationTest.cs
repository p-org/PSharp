// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class EntryPointMachineCreationTest : BaseTest
    {
        class M : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        class N : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestEntryPointMachineCreation()
        {
            var test = new Action<PSharpRuntime>((r) => {
                MachineId m = r.CreateMachine(typeof(M));
                MachineId n = r.CreateMachine(typeof(N));
                r.Assert(m != null && m != null, "Machine ids are null.");
            });

            base.AssertSucceeded(test);
        }
    }
}
