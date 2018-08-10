//-----------------------------------------------------------------------
// <copyright file="MachineStateInheritanceTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class MachineStateInheritanceTest : BaseTest
    {
        public MachineStateInheritanceTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventDoAction(typeof(E), nameof(Check))]
            abstract class BaseState : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void Check()
            {
                Assert(false, "Error reached.");
            }
        }

        class M2 : Machine
        {
            [Start]
            class Init : BaseState { }

            [Start]
            class BaseState : MachineState { }
        }

        class M3 : Machine
        {
            [Start]
            class Init : BaseState { }

            [OnEntry(nameof(BaseOnEntry))]
            class BaseState : MachineState { }

            void BaseOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEntry(nameof(BaseOnEntry))]
            class BaseState : MachineState { }

            void InitOnEntry() { }

            void BaseOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventDoAction(typeof(E), nameof(Check))]
            class BaseState : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void Check()
            {
                Assert(false, "Error reached.");
            }
        }

        class M6 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            class BaseState : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void Check() { }

            void BaseCheck()
            {
                Assert(false, "Error reached.");
            }
        }

        class M7 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            class BaseState : BaseBaseState { }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            class BaseBaseState : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void Check() { }

            void BaseCheck()
            {
                Assert(false, "Error reached.");
            }

            void BaseBaseCheck()
            {
                Assert(false, "Error reached.");
            }
        }

        class M8 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            class BaseState : BaseBaseState { }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            class BaseBaseState : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void BaseCheck() { }

            void BaseBaseCheck()
            {
                Assert(false, "Error reached.");
            }
        }

        class M9 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventGotoState(typeof(E), typeof(Done))]
            class BaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }
        }

        class M10 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventGotoState(typeof(E), typeof(Error))]
            class BaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            [OnEntry(nameof(ErrorOnEntry))]
            class Error : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }

            void ErrorOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M11 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventGotoState(typeof(E), typeof(Error))]
            class BaseState : BaseBaseState { }

            [OnEventGotoState(typeof(E), typeof(Error))]
            class BaseBaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            [OnEntry(nameof(ErrorOnEntry))]
            class Error : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }

            void ErrorOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M12 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventGotoState(typeof(E), typeof(Done))]
            class BaseState : BaseBaseState { }

            [OnEventGotoState(typeof(E), typeof(Error))]
            class BaseBaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            [OnEntry(nameof(ErrorOnEntry))]
            class Error : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }

            void ErrorOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M13 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventPushState(typeof(E), typeof(Done))]
            class BaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }
        }

        class M14 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventPushState(typeof(E), typeof(Error))]
            class BaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            [OnEntry(nameof(ErrorOnEntry))]
            class Error : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }

            void ErrorOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M15 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventPushState(typeof(E), typeof(Error))]
            class BaseState : BaseBaseState { }

            [OnEventPushState(typeof(E), typeof(Error))]
            class BaseBaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            [OnEntry(nameof(ErrorOnEntry))]
            class Error : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }

            void ErrorOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        class M16 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : BaseState { }

            [OnEventPushState(typeof(E), typeof(Done))]
            class BaseState : BaseBaseState { }

            [OnEventPushState(typeof(E), typeof(Error))]
            class BaseBaseState : MachineState { }

            [OnEntry(nameof(DoneOnEntry))]
            class Done : MachineState { }

            [OnEntry(nameof(ErrorOnEntry))]
            class Error : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void DoneOnEntry()
            {
                Assert(false, "Done reached.");
            }

            void ErrorOnEntry()
            {
                Assert(false, "Error reached.");
            }
        }

        [Fact]
        public void TestMachineStateInheritingAbstractState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            var bugReport = "Error reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateInheritingStateDuplicateStart()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2));
            });

            var bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.MachineStateInheritanceTest+M2()' " +
                "can not declare more than one start states.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEntry()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            var bugReport = "Error reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEntry()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M4));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEventDoAction()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5));
            });

            var bugReport = "Error reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEventDoAction()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M6));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateOverridingTwoStatesOnEventDoAction()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M7));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateOverridingDeepStateOnEventDoAction()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M8));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEventGotoState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M9));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEventGotoState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M10));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingTwoStatesOnEventGotoState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M11));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingDeepStateOnEventGotoState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M12));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateInheritingStateOnEventPushState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M13));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingStateOnEventPushState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M14));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingTwoStatesOnEventPushState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M15));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }

        [Fact]
        public void TestMachineStateOverridingDeepStateOnEventPushState()
        {
            var test = new Action<IPSharpRuntime>((r) =>
            {
                r.CreateMachine(typeof(M16));
            });

            var bugReport = "Done reached.";
            AssertFailed(test, bugReport, false);
        }
    }
}
