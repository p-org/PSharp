// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class EntryPointMachineCreationTest : BaseTest
    {
        public EntryPointMachineCreationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : Machine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        private class N : Machine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact]
        public void TestEntryPointMachineCreation()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                MachineId m = r.CreateMachine(typeof(M));
                MachineId n = r.CreateMachine(typeof(N));
                r.Assert(m != null && m != null, "Machine ids are null.");
            });

            this.AssertSucceeded(test);
        }
    }
}
