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
    public class TwoMachineIntegrationTests : BaseTest
    {
        public TwoMachineIntegrationTests(ITestOutputHelper output)
            : base(output)
        { }

        class E1 : Event
        {
            public E1()
                : base(1, -1)
            {
            }
        }

        class E2 : Event
        {
            public E2()
                : base(1, -1)
            {
            }
        }

        class E3 : Event
        {
            public bool Value;

            public E3(bool value)
                : base(1, -1)
            {
                this.Value = value;
            }
        }

        class E4 : Event
        {
            public MachineId Id;

            public E4(MachineId id)
                : base(1, -1)
            {
                this.Id = id;
            }
        }

        class SuccessE : Event { }

        class IgnoredE : Event { }

        class M1 : Machine
        {
            bool Test = false;
            MachineId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(Default), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.TargetId = this.CreateMachine(typeof(M2));
                this.Raise(new E1());
            }

            void InitOnExit()
            {
                this.Send(this.TargetId, new E3(this.Test));
            }

            class S1 : MachineState { }

            void Action1()
            {
                this.Test = true;
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(EntryAction))]
            class Init : MachineState { }

            void InitOnEntry() { }

            void EntryAction()
            {
                if (this.ReceivedEvent.GetType() == typeof(E3))
                {
                    Action2();
                }
            }

            void Action2()
            {
                this.Assert((this.ReceivedEvent as E3).Value == false);
            }
        }

        class M3 : Machine
        {
            MachineId TargetId;
            int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                TargetId = this.CreateMachine(typeof(M4));
                this.Raise(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(WaitEvent))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                Count = Count + 1;
                if (Count == 1)
                {
                    this.Send(TargetId, new E4(this.Id));
                }

                if (Count == 2)
                {
                    this.Send(TargetId, new IgnoredE());
                }

                this.Raise(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            class WaitEvent : MachineState { }

            class Done : MachineState { }
        }

        class M4 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            class Waiting : MachineState { }

            void Action1()
            {
                this.Assert(false);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send((this.ReceivedEvent as E4).Id, new E1());
                this.Raise(new SuccessE());
            }
        }

        class M5 : Machine
        {
            MachineId TargetId;
            int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                TargetId = this.CreateMachine(typeof(M6));
                this.Raise(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(WaitEvent))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                Count = Count + 1;
                if (Count == 1)
                {
                    this.Send(TargetId, new E4(this.Id));
                }

                if (Count == 2)
                {
                    this.Send(TargetId, new Halt());
                    this.Send(TargetId, new IgnoredE());
                }

                this.Raise(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            class WaitEvent : MachineState { }

            class Done : MachineState { }
        }

        class M6 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventGotoState(typeof(Halt), typeof(Inactive))]
            class Waiting : MachineState { }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send((this.ReceivedEvent as E4).Id, new E1());
                this.Raise(new SuccessE());
            }

            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            [IgnoreEvents(typeof(E4))]
            class Inactive : MachineState { }

            void Action1()
            {
                this.Assert(false);
            }
        }

        class M7 : Machine
        {
            MachineId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                TargetId = this.CreateMachine(typeof(M8));
                this.Raise(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send(TargetId, new E4(this.Id));
                this.Raise(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            class Waiting : MachineState { }

            class Done : MachineState { }
        }

        class M8 : Machine
        {
            int Count2 = 0;

            [Start]
            [OnEntry(nameof(EntryWaitPing))]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            class Waiting : MachineState { }

            void EntryWaitPing() { }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            [OnEventDoAction(typeof(Halt), nameof(Action1))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                Count2 = Count2 + 1;

                if (Count2 == 1)
                {
                    this.Send((this.ReceivedEvent as E4).Id, new E1());
                }

                if (Count2 == 2)
                {
                    this.Send((this.ReceivedEvent as E4).Id, new E1());
                    this.Raise(new Halt());
                    return;
                }

                this.Raise(new SuccessE());
            }

            void Action1()
            {
                this.Assert(false);
            }
        }

        class M9 : Machine
        {
            MachineId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new E1());
            }

            void InitOnExit()
            {
                this.TargetId = this.CreateMachine(typeof(M10));
                this.Send(this.TargetId, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send(this.TargetId, new E2());
            }
        }

        class M10 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            class Init : MachineState { }

            void HandleE1()
            {
            }

            void HandleE2()
            {
                this.Assert(false);
            }
        }

        [Fact]
        public void TestTwoMachineIntegration1()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            base.AssertFailed(configuration, test, 1, true);
        }

        [Fact]
        public void TestTwoMachineIntegration2()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            base.AssertFailed(configuration, test, 1, true);
        }

        [Fact]
        public void TestTwoMachineIntegration3()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M5));
            });

            base.AssertFailed(configuration, test, 1, true);
        }

        [Fact]
        public void TestTwoMachineIntegration4()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M7));
            });

            base.AssertFailed(configuration, test, 1, true);
        }

        [Fact]
        public void TestTwoMachineIntegration5()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M9));
            });

            base.AssertFailed(test, 1, true);
        }
    }
}
