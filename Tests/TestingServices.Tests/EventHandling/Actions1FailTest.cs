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
    public class Actions1FailTest : BaseTest
    {
        public Actions1FailTest(ITestOutputHelper output)
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
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E2), typeof(S1))] // exit actions are performed before transition to S1
            [OnEventDoAction(typeof(E4), nameof(Action1))] // E4, E3 have no effect on reachability of assert(false)
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateMachine(typeof(Ghost));
                this.Send(this.GhostMachine, new Config(this.Id));
                this.Send(this.GhostMachine, new E1(), new SendOptions(assert: 1));
            }

            private void ExitInit()
            {
                this.Test = true;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Assert(this.Test == true); // holds
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // this assert is reachable: Real -E1-> Ghost -E2-> Real;
                // then Real_S1 (assert holds), Real_S2 (assert fails)
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
                this.Send(this.RealMachine, new E4(), new SendOptions(assert: 1));
                this.Send(this.RealMachine, new E2(), new SendOptions(assert: 1));
            }

            private class S2 : MachineState
            {
            }
        }

        /// <summary>
        /// Tests basic semantics of actions and goto transitions.
        /// </summary>
        [Fact(Timeout=5000)]
        public void TestActions1Fail()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Real)); });
            this.AssertFailed(configuration, test, 1, true);
        }
    }
}
