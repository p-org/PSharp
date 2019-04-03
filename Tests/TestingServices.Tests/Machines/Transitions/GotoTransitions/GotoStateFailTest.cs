// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class GotoStateFailTest : BaseTest
    {
        public GotoStateFailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Program : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                // This line no longer builds after converting from Goto(typeof(T)) to Goto<T>()
                // due to the "where T: MachineState" constraint on Goto<T>().
                // this.Goto<object>();

                // Added a different failure mode here; try to Goto a state from another machine.
                this.Goto<Program2.Done>();
            }

            private class Done : MachineState
            {
            }
        }

        private class Program2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            internal class Done : MachineState
            {
            }
        }

        [Fact]
        public void TestGotoStateFail()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(Program));
            });

            this.AssertFailed(test, 1, true);
        }
    }
}
