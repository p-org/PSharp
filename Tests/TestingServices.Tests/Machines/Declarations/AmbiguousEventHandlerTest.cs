// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class AmbiguousEventHandlerTest : BaseTest
    {
        public AmbiguousEventHandlerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class Program : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void HandleE()
            {
            }

#pragma warning disable CA1801 // Parameter not used
            private void HandleE(int k)
            {
            }
#pragma warning restore CA1801 // Parameter not used
        }

        private class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MonitorState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E());
            }

            private void HandleE()
            {
            }

#pragma warning disable CA1801 // Parameter not used
            private void HandleE(int k)
            {
            }
#pragma warning restore CA1801 // Parameter not used
        }

        [Fact]
        public void TestAmbiguousMachineEventHandler()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(Program));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestAmbiguousMonitorEventHandler()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Safety));
            });

            this.AssertSucceeded(test);
        }
    }
}
