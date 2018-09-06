﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class DuplicateEventHandlersTest : BaseTest
    {
        class E : Event { }

        class M1: Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check1))]
            [OnEventDoAction(typeof(E), nameof(Check2))]
            class Init : MachineState { }

            void Check1() { }

            void Check2() { }
        }

        class M2 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventGotoState(typeof(E), typeof(S2))]
            class Init : MachineState { }

            class S1 : MachineState { }

            class S2 : MachineState { }
        }

        class M3 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            class Init : MachineState { }

            class S1 : MachineState { }

            class S2 : MachineState { }
        }

        class M4 : Machine
        {
            [Start]
            class Init : BaseState { }

            [OnEventDoAction(typeof(E), nameof(Check1))]
            [OnEventDoAction(typeof(E), nameof(Check2))]
            class BaseState : MachineState { }

            void Check1() { }

            void Check2() { }
        }

        class M5 : Machine
        {
            [Start]
            class Init : BaseState { }

            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventGotoState(typeof(E), typeof(S2))]
            class BaseState : MachineState { }

            class S1 : MachineState { }

            class S2 : MachineState { }
        }

        class M6 : Machine
        {
            [Start]
            class Init : BaseState { }

            [OnEventPushState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            class BaseState : MachineState { }

            class S1 : MachineState { }

            class S2 : MachineState { }
        }

        class M7 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            class Init : MachineState { }

            class S1 : MachineState { }

            class S2 : MachineState { }

            void Check() { }
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerDo()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M1()' declared multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M1+Init'.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerGoto()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M2()' declared multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M2+Init'.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerPush()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M3()' declared multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M3+Init'.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerInheritanceDo()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M4));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M4()' inherited multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' from state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M4+BaseState' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M4+Init'.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerInheritanceGoto()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M5()' inherited multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' from state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M5+BaseState' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M5+Init'.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerInheritancePush()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M6));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M6()' inherited multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' from state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M6+BaseState' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M6+Init'.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerMixed()
        {
            var test = new Action<PSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M7));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M7()' declared multiple " +
                "handlers for 'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+E' in state " +
                "'Microsoft.PSharp.TestingServices.Tests.Unit.DuplicateEventHandlersTest+M7+Init'.";
            AssertFailed(test, bugReport, false);
        }
    }
}
