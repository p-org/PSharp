// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    /// <summary>
    /// Tests that event payload works correctly with a push transition.
    /// </summary>
    public class Actions6FailTest : BaseTest
    {
        public Actions6FailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
                : base(-1, -1)
            {
                this.Id = id;
            }
        }

        private class E1 : Event
        {
            public E1()
                : base(1, -1)
            {
            }
        }

        private class E2 : Event
        {
            public int Value;

            public E2(int value)
                : base(1, -1)
            {
                this.Value = value;
            }
        }

        private class E3 : Event
        {
            public E3()
                : base(1, -1)
            {
            }
        }

        private class E4 : Event
        {
            public E4()
                : base(1, -1)
            {
            }
        }

        private class Unit : Event
        {
            public Unit()
                : base(1, -1)
            {
            }
        }

        private class Real : Machine
        {
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateMachine(typeof(Ghost));
                this.Send(this.GhostMachine, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.GhostMachine, new E1());
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // this assert is reachable
                this.Assert(false);
            }

            private void Action1()
            {
                this.Send(this.GhostMachine, new E3());
            }
        }

        private class Ghost : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E2(100));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4());
            }
        }

        [Fact]
        public void TestActions6Fail()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(Real)); });
            this.AssertFailed(configuration, test, 1, true);
        }
    }
}
