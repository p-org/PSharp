// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OneMachineIntegrationTests : BaseTest
    {
        public OneMachineIntegrationTests(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

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
            public E3()
                : base(1, -1)
            {
            }
        }

        class M1 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E2());
                this.Raise(new E1());
            }

            void HandleE1()
            {
                this.Test = true;
            }

            void HandleE2()
            {
                this.Assert(this.Test == false);
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E2());
            }

            void HandleE2()
            {
                this.Assert(false);
            }
        }

        class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            class Active : MachineState { }
        }

        class M4 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
            }

            void HandleE2()
            {
                this.Assert(this.Test == false);
            }
        }

        class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            void HandleE2()
            {
                this.Assert(false);
            }
        }

        class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Init))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }
        }

        class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Assert(false);
            }
        }

        class M8 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3());
            }

            void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        class M9 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3());
                this.Pop();
            }

            void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        class M10 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3());
            }

            void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        class M11 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            [OnEventGotoState(typeof(E3), typeof(Checking))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3());
            }

            void ActiveOnExit()
            {
                this.Send(this.Id, new E3());
            }

            [OnEntry(nameof(CheckingOnEntry))]
            class Checking : MachineState { }

            void CheckingOnEntry()
            {
                this.Assert(this.Test == false);
            }
        }

        class M12 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3());
            }

            void ActiveOnExit()
            {
                this.Assert(this.Test == false);
            }

            void HandleE3() { }
        }

        class M13 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
                this.Pop();
            }

            void ActiveOnExit()
            {
                this.Send(this.Id, new E3());
            }

            void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        class M14 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void InitOnExit()
            {
                this.Send(this.Id, new E2());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Raise(new E1());
            }

            void HandleE3() { }
        }

        class M15 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new E());
            }

            void InitOnExit() { }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Pop();
            }

            void ActiveOnExit()
            {
                this.Assert(false);
            }
        }

        class M16 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(Halt), typeof(Active))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
                this.Raise(new Halt());
            }

            void InitOnExit() { }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Test = true;
            }

            void HandleE1()
            {
                this.Assert(this.Test == false);
            }
        }

        class M17 : Machine
        {
            bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(Default), typeof(Active))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new E2());
            }

            void InitOnExit() { }

            void HandleE1()
            {
                this.Test = true;
            }

            void HandleE2()
            {
                this.Send(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Assert(this.Test == false);
            }
        }

        class M18 : Machine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(Default), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry() { }

            void InitOnExit() { }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Assert(this.Test == true);
            }
        }

        class M19 : Machine
        {
            int Value;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E), typeof(Active))]
            [OnEventDoAction(typeof(Default), nameof(DefaultAction))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Value = 0;
                this.Raise(new E());
            }

            void InitOnExit() { }

            void DefaultAction()
            {
                this.Assert(false);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [IgnoreEvents(typeof(E))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                if (this.Value == 0)
                {
                    this.Raise(new E());
                }
                else
                {
                    this.Value++;
                }
            }

            void ActiveOnExit() { }
        }

        class M20 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(Default), typeof(Active))]
            class Init : MachineState { }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Assert(this.ReceivedEvent.GetType() == typeof(Default));
            }
        }

        [Fact]
        public void TestOneMachineIntegration1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration3()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration4()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M4));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration5()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M5));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration6()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M6));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration7()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M7));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration8()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M8));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration9()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M9));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration10()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M10));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration11()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M11));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration12()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M12));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration13()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M13));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration14()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M14));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration15()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M15));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration16()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M16));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration17()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M17));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration18()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M18));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration19()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M19));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestOneMachineIntegration20()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M20));
            });

            base.AssertSucceeded(test);
        }
    }
}
