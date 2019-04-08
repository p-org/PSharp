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
    public class MaxInstances1FailTest : BaseTest
    {
        public MaxInstances1FailTest(ITestOutputHelper output)
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
                : base(-1, -1)
            {
            }
        }

        private class E4 : Event
        {
        }

        private class Unit : Event
        {
            public Unit()
                : base(1, -1)
            {
            }
        }

        private class RealMachine : Machine
        {
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateMachine(typeof(GhostMachine));
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
                this.Send(this.GhostMachine, new E1()); // error
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(Unit), typeof(S3))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(E4), typeof(S3))]
            private class S3 : MachineState
            {
            }

            private void Action1()
            {
                this.Assert((this.ReceivedEvent as E2).Value == 100);
                this.Send(this.GhostMachine, new E3());
                this.Send(this.GhostMachine, new E3());
            }
        }

        private class GhostMachine : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(GhostInit))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class GhostInit : MachineState
            {
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            [IgnoreEvents(typeof(E1))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E2(100));
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(E3), typeof(GhostInit))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4());
                this.Send(this.RealMachine, new E4());
                this.Send(this.RealMachine, new E4());
            }
        }

        [Fact(Timeout=5000)]
        public void TestMaxInstances1AssertionFailure()
        {
            var configuration = GetConfiguration();
            configuration.ReductionStrategy = ReductionStrategy.None;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.MaxSchedulingSteps = 6;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(RealMachine)); });
            this.AssertFailed(configuration, test, 1, true);
        }
    }
}
