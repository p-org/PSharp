// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GenericMachineTest : BaseTest
    {
        class M<T> : Machine
        {
            T Item;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Item = default(T);
                this.Goto<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            class Active : MachineState { }

            void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        class N : M<int>
        {

        }

        [Fact]
        public void TestGenericMachine1()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(M<int>)); });
            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestGenericMachine2()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(N)); });
            base.AssertSucceeded(test);
        }
    }
}
