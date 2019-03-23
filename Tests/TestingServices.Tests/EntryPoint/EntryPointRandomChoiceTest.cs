// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class EntryPointRandomChoiceTest : BaseTest
    {
        public EntryPointRandomChoiceTest(ITestOutputHelper output)
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

        [Fact]
        public void TestEntryPointRandomChoice()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                if (r.Random())
                {
                    r.CreateMachine(typeof(M));
                }
            });

            this.AssertSucceeded(test);
        }
    }
}
