// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class EntryPointThrowExceptionTest : BaseTest
    {
        public EntryPointThrowExceptionTest(ITestOutputHelper output)
            : base(output)
        { }

        class M : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestEntryPointThrowException()
        {
            var test = new Action<PSharpRuntime>((r) => {
                MachineId m = r.CreateMachine(typeof(M));
                throw new InvalidOperationException();
            });

            base.AssertFailedWithException(test, typeof(InvalidOperationException), true);
        }

        [Fact]
        public void TestEntryPointNoMachinesThrowException()
        {
            var test = new Action<PSharpRuntime>((r) => {
                throw new InvalidOperationException();
            });

            base.AssertFailedWithException(test, typeof(InvalidOperationException), true);
        }
    }
}
