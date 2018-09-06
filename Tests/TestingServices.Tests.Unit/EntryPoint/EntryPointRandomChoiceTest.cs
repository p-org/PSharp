// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class EntryPointRandomChoiceTest : BaseTest
    {
        class M : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestEntryPointRandomChoice()
        {
            var test = new Action<PSharpRuntime>((r) => {
                if (r.Random())
                {
                    r.CreateMachine(typeof(M));
                }
            });

            base.AssertSucceeded(test);
        }
    }
}
