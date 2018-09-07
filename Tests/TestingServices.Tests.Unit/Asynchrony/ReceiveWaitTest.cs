// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class ReceiveWaitTest : BaseTest
    {
        public ReceiveWaitTest(ITestOutputHelper output)
               : base(output)
        { }

        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E());
                this.Receive(typeof(E)).Wait();
                this.Assert(false);
            }
        }

        [Fact]
        public void TestAsyncReceiveEvent()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            var bugReport = "Detected an assertion failure.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
