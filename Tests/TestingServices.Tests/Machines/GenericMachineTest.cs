﻿// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class GenericMachineTest : BaseTest
    {
        public GenericMachineTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M<T> : Machine
        {
            private T Item;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Item = default;
                this.Goto<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            private class Active : MachineState
            {
            }

            private void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        private class N : M<int>
        {
        }

        [Fact(Timeout=5000)]
        public void TestGenericMachine1()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M<int>));
            });
        }

        [Fact(Timeout=5000)]
        public void TestGenericMachine2()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(N));
            });
        }
    }
}
