// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies.DPOR;
using Microsoft.PSharp.TestingServices.StateCaching;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OnEventUnhandledTest : BaseTest
    {
        public OnEventUnhandledTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            private class S : MachineState
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(currentState == "S");
                this.Assert(false, "OnEventUnhandled called");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HaltMachine;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledCalled()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new E());
            },
            expectedError: "OnEventUnhandled called");
        }

        private class M2 : Machine
        {
            private int x = 0;

            [Start]
            private class S : MachineState
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.x == 0);
                this.x++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(this.x == 1);
                return OnExceptionOutcome.HaltMachine;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnExceptionCalledSecond()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
            });
        }

        private class M3 : Machine
        {
            private int x = 0;

            [Start]
            private class S : MachineState
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.x == 0, "OnEventUnhandledAsync not called first");
                this.x++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(this.x == 1, "OnException not called second");
                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventUnhandledExceptionPropagation()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new E());
            },
            expectedError: "Machine 'M3()' received event 'E' that cannot be handled.");
        }

        private class M4 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Foo))]
            private class S : MachineState
            {
            }

            private void Foo()
            {
                throw new Exception();
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(false, "OnEventUnhandledAsync called");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HaltMachine;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledNotCalled()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E());
            });
        }
    }
}
