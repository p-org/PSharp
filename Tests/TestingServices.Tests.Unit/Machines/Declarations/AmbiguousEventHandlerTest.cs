// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class AmbiguousEventHandlerTest : BaseTest
    {
        public AmbiguousEventHandlerTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            void HandleE() { }
            void HandleE(int k) { }
        }

        class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MonitorState { }

            void InitOnEntry()
            {
                this.Raise(new E());
            }

            void HandleE() { }
            void HandleE(int k) { }
        }

        [Fact]
        public void TestAmbiguousMachineEventHandler()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestAmbiguousMonitorEventHandler()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Safety));
            });

            base.AssertSucceeded(test);
        }
    }
}
