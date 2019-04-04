// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class DuplicateEventHandlersTest : BaseTest
    {
        public DuplicateEventHandlersTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check1))]
            [OnEventDoAction(typeof(E), nameof(Check2))]
            private class Init : MachineState
            {
            }

            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class Init : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            private class Init : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M4 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check1))]
            [OnEventDoAction(typeof(E), nameof(Check2))]
            private class BaseState : MachineState
            {
            }

            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        private class M5 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class BaseState : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M6 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            private class BaseState : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M7 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            private class Init : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }

            private void Check()
            {
            }
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerDo()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            var bugReport = "Machine 'M1()' declared multiple handlers for 'E' in state 'M1+Init'.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerGoto()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2));
            });

            var bugReport = "Machine 'M2()' declared multiple handlers for 'E' in state 'M2+Init'.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerPush()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            var bugReport = "Machine 'M3()' declared multiple handlers for 'E' in state 'M3+Init'.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerInheritanceDo()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M4));
            });

            var bugReport = "Machine 'M4()' inherited multiple handlers for 'E' from state " +
                "'M4+BaseState' in state 'M4+Init'.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerInheritanceGoto()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5));
            });

            var bugReport = "Machine 'M5()' inherited multiple handlers for 'E' from state " +
                "'M5+BaseState' in state 'M5+Init'.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerInheritancePush()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M6));
            });

            var bugReport = "Machine 'M6()' inherited multiple handlers for 'E' from state " +
                "'M6+BaseState' in state 'M6+Init'.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineDuplicateEventHandlerMixed()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M7));
            });

            var bugReport = "Machine 'M7()' declared multiple handlers for 'E' in state 'M7+Init'.";
            this.AssertFailed(test, bugReport, false);
        }
    }
}
