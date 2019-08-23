﻿using System;
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

        [Fact(Timeout=5000)]
        public void TestEntryPointRandomChoice()
        {
            this.Test(r =>
            {
                if (r.Random())
                {
                    r.CreateMachine(typeof(M));
                }
            });
        }
    }
}
