// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MachineStateInheritanceTest : BaseTest
    {
        public MachineStateInheritanceTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check))]
            private abstract class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void Check()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M2 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [Start]
            private class BaseState : MachineState
            {
            }
        }

        private class M3 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEntry(nameof(BaseOnEntry))]
            private class BaseState : MachineState
            {
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEntry(nameof(BaseOnEntry))]
            private class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check))]
            private class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void Check()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void Check()
            {
            }

            private void BaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M7 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            private class BaseBaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void Check()
            {
            }

            private void BaseCheck()
            {
                this.Assert(false, "Error reached.");
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M8 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            private class BaseBaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void BaseCheck()
            {
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M9 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Done))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }
        }

        private class M10 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M11 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M12 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M13 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Done))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }
        }

        private class M14 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M15 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M16 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        [Fact]
        public void TestMachineStateInheritingAbstractState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            var bugReport = "Error reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateInheritingStateDuplicateStart()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2));
            });

            var bugReport = "Machine 'M2()' can not declare more than one start states.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEntry()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            var bugReport = "Error reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEntry()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M4));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEventDoAction()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5));
            });

            var bugReport = "Error reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEventDoAction()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M6));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateOverridingTwoStatesOnEventDoAction()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M7));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateOverridingDeepStateOnEventDoAction()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M8));
            });

            this.AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEventGotoState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M9));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEventGotoState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M10));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingTwoStatesOnEventGotoState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M11));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingDeepStateOnEventGotoState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M12));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEventPushState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M13));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEventPushState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M14));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingTwoStatesOnEventPushState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M15));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingDeepStateOnEventPushState()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M16));
            });

            var bugReport = "Done reached.";
            this.AssertFailed(test, bugReport, false);
        }
    }
}
