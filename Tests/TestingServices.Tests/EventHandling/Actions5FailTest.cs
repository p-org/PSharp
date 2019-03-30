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
    /// Tests the semantics of push transitions and inheritance of actions.
    /// </summary>
    public class Actions5FailTest : BaseTest
    {
        public Actions5FailTest(ITestOutputHelper output)
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
            public E2()
                : base(1, -1)
            {
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

                // we wait in this state until E2 comes from Ghost,
                // then handle E2 using the inherited handler Action1
                // installed by Init
                // then wait until E4 comes from Ghost, and since
                // there's no handler for E4 in this pushed state,
                // this state is popped, and E4 goto handler from Init
                // is invoked
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
                this.Send(this.RealMachine, new E2());
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
        public void TestActions5Fail()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(Real)); });
            this.AssertFailed(configuration, test, 1, true);
        }
    }
}
