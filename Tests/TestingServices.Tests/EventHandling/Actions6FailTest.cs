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
            {
                this.Id = id;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public int Value;

            public E2(int value)
            {
                this.Value = value;
            }
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class Unit : Event
        {
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
                this.Send(this.GhostMachine, new E1(), new SendOptions(assert: 1));
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
                this.Send(this.GhostMachine, new E3(), new SendOptions(assert: 1));
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
                this.Send(this.RealMachine, new E2(100), new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4(), new SendOptions(assert: 1));
            }
        }

        [Fact(Timeout=5000)]
        public void TestActions6Fail()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Real)); });
            this.AssertFailed(configuration, test, 1, true);
        }
    }
}
