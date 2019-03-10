// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class GotoStateTopLevelActionFailTest : BaseTest
    {
        public GotoStateTopLevelActionFailTest(ITestOutputHelper output)
            : base(output)
        { }

        public enum ErrorType
        {
            CALL_GOTO,
            CALL_PUSH,
            CALL_RAISE,
            CALL_SEND,
            ON_EXIT
        };

        class Configure : Event
        {
            public ErrorType ErrorTypeVal;

            public Configure(ErrorType errorTypeVal)
            {
                this.ErrorTypeVal = errorTypeVal;
            }
        }

        class E : Event { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var errorType = (this.ReceivedEvent as Configure).ErrorTypeVal;
                this.Foo();
                switch(errorType)
                {
                    case ErrorType.CALL_GOTO:
                        this.Goto<Done>();
                        break;
                    case ErrorType.CALL_PUSH:
                        this.Push<Done>();
                        break;
                    case ErrorType.CALL_RAISE:
                        this.Raise(new E());
                        break;
                    case ErrorType.CALL_SEND:
                        this.Send(Id, new E());
                        break;
                    case ErrorType.ON_EXIT:
                        break;
                }
            }

            void ExitMethod()
            {
                this.Pop();
            }

            void Foo()
            {
                this.Goto<Done>();
            }

            class Done : MachineState { }
        }

        [Fact]
        public void TestGotoStateTopLevelActionFail1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_GOTO));
            });

            var bugReport = "Machine 'Program()' has called multiple raise, goto, push or pop in the same action.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoStateTopLevelActionFail2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_RAISE));
            });

            var bugReport = "Machine 'Program()' has called multiple raise, goto, push or pop in the same action.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoStateTopLevelActionFail3()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_SEND));
            });

            var bugReport = "Machine 'Program()' cannot call 'Send' after calling raise, goto, push or pop in the same action.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoStateTopLevelActionFail4()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.ON_EXIT));
            });

            var bugReport = "Machine 'Program()' has called raise, goto, push or pop inside an OnExit method.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoStateTopLevelActionFail5()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_PUSH));
            });

            var bugReport = "Machine 'Program()' has called multiple raise, goto, push or pop in the same action.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}