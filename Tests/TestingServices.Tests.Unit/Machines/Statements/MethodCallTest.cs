// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class MethodCallTest : BaseTest
    {
        public MethodCallTest(ITestOutputHelper output)
               : base(output)
        { }

        class E : Event { }

        class Program : Machine
        {
            int x;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                x = 2;
                this.Foo(1, 3, x);
            }

            int Foo(int x, int y, int z)
            {
                return 0;
            }
        }

        [Fact]
        public void TestMethodCall()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertSucceeded(test);
        }
    }
}
