// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class CompletenessTest : BaseTest
    {
        public CompletenessTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class P : Monitor
        {
            [Cold]
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Fail))]
            [OnEventGotoState(typeof(E2), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            [IgnoreEvents(typeof(E1), typeof(E2))]
            private class S2 : MonitorState
            {
            }

            private void Fail()
            {
                this.Assert(false);
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Monitor<P>(new E1());
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Monitor<P>(new E2());
            }
        }

        [Fact]
        public void TestCompleteness1()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(P));
                r.CreateMachine(typeof(M2));
                r.CreateMachine(typeof(M1));
            });

            var config = Configuration.Create().WithNumberOfIterations(100);

            this.AssertFailed(config, test, 1, true);
        }

        [Fact]
        public void TestCompleteness2()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(P));
                r.CreateMachine(typeof(M1));
                r.CreateMachine(typeof(M2));
            });

            var config = Configuration.Create().WithNumberOfIterations(100);

            this.AssertFailed(config, test, 1, true);
        }
    }
}
