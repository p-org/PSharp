// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine25Test : BaseTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public E2() : base(1, -1) { }
        }

        class E3 : Event
        {
            public E3() : base(1, -1) { }
        }

        class E4 : Event
        {
            public E4() : base(1, -1) { }
        }

        class Unit : Event
        {
            public Unit() : base(1, -1) { }
        }

        class Real : Machine
        {
            MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E2), typeof(S1))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Raise(new E2());
            }

            void ExitInit()
            {
                GhostMachine = this.CreateMachine(typeof(Ghost));
                this.Send(GhostMachine, new Config(this.Id));
            }

            [OnEntry(nameof(EntryS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Send(GhostMachine, new E1());
            }
        }

        class Ghost : Machine
        {
            MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventDoAction(typeof(E1), nameof(Action))]
            class Init : MachineState { }

            void Configure()
            {
                RealMachine = (this.ReceivedEvent as Config).Id;
            }

            void Action()
            {
                this.Assert(false);
            }
        }

        /// <summary>
        /// P# semantics test: one machine, "new" in exit function.
        /// </summary>
        [Fact]
        public void TestNewInExit()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real));
            });

            base.AssertFailed(test, 1);
        }
    }
}
