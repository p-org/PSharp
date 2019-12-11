﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OnHaltTest : BaseTest
    {
        public OnHaltTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public MachineId Id;

            public E()
            {
            }

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Assert(false);
            }
        }

        private class M2a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Receive(typeof(Event)).Wait();
            }
        }

        private class M2b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Raise(new E());
            }
        }

        private class M2c : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Goto<Init>();
            }
        }

        private class Dummy : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        private class M3 : Machine
        {
            private MachineId sender;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.sender = (this.ReceivedEvent as E).Id;
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                // no-ops but no failure
                this.Send(this.sender, new E());
                this.Random();
                this.Assert(true);
                this.CreateMachine(typeof(Dummy));
            }
        }

        private class M4 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestHaltCalled()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestReceiveOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2a));
            },
            expectedError: "Machine 'M2a()' invoked Receive while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestRaiseOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2b));
            },
            expectedError: "Machine 'M2b()' invoked Raise while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2c));
            },
            expectedError: "Machine 'M2c()' invoked Goto while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestAPIsOnHalt()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M4));
                r.CreateMachine(typeof(M3), new E(m));
            });
        }
    }
}
