﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class PopTest : BaseTest
    {
        public PopTest(ITestOutputHelper output)
            : base(output)
        { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(Init))]
            public class S1 : MachineState { }

            void Init()
            {
                this.Pop();
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(Init))]
            [OnExit(nameof(Exit))]
            public class S1 : MachineState { }

            void Init()
            {
                this.Goto<S2>();
            }

            void Exit()
            {
                this.Pop();
            }

            public class S2 : MachineState { }
        }

        [Fact]
        public void TestUnbalancedPop()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(M), "M"); });
            var bugReport = "Machine 'M()' popped with no matching push.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestPopDuringOnExit()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(N), "N"); });
            var bugReport = "Machine 'N()' has called raise, goto, push or pop inside an OnExit method.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
