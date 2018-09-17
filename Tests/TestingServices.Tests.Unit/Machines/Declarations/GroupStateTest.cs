// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GroupStateTest : BaseTest
    {
        public GroupStateTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class Program : Machine
        {
            class States1 : StateGroup
            {
                [Start]
                [OnEntry(nameof(States1S1OnEntry))]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MachineState { }

                [OnEntry(nameof(States1S2OnEntry))]
                [OnEventGotoState(typeof(E), typeof(States2.S1))]
                public class S2 : MachineState { }
            }

            class States2 : StateGroup
            {
                [OnEntry(nameof(States2S1OnEntry))]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MachineState { }

                [OnEntry(nameof(States2S2OnEntry))]
                public class S2 : MachineState { }
            }

            void States1S1OnEntry()
            {
                this.Raise(new E());
            }

            void States1S2OnEntry()
            {
                this.Raise(new E());
            }

            void States2S1OnEntry()
            {
                this.Raise(new E());
            }

            void States2S2OnEntry()
            {
                this.Monitor<M>(new E());
            }
        }

        class M : Monitor
        {
            class States1 : StateGroup
            {
                [Start]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MonitorState { }

                [OnEntry(nameof(States1S2OnEntry))]
                [OnEventGotoState(typeof(E), typeof(States2.S1))]
                public class S2 : MonitorState { }
            }

            class States2 : StateGroup
            {
                [OnEntry(nameof(States2S1OnEntry))]
                [OnEventGotoState(typeof(E), typeof(S2))]
                public class S1 : MonitorState { }

                [OnEntry(nameof(States2S2OnEntry))]
                public class S2 : MonitorState { }
            }

            void States1S2OnEntry()
            {
                this.Raise(new E());
            }

            void States2S1OnEntry()
            {
                this.Raise(new E());
            }

            void States2S2OnEntry()
            {
                this.Assert(false);
            }
        }

        [Fact]
        public void TestGroupState()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(M));
                r.CreateMachine(typeof(Program));
            });

            var bugReport = "Detected an assertion failure.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
