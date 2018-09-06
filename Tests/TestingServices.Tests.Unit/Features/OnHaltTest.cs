// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class OnHaltTest : BaseTest
    {
        class E : Event
        {
            public MachineId Id;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Assert(false);
            }
        }

        class M2a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Receive(typeof(Event)).Wait();
            }
        }

        class M2b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Raise(new E());
            }
        }

        class M2c : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Goto<Init>();
            }
        }

        class Dummy : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        class M3 : Machine
        {
            MachineId sender;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                sender = (this.ReceivedEvent as E).Id;
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                // no-ops but no failure
                this.Send(sender, new E());
                this.Random();
                this.Assert(true);
                this.CreateMachine(typeof(Dummy));
            }
        }

        class M4 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }
        }

        [Fact]
        public void TestHaltCalled()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestReceiveOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2a));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2a()' invoked Receive while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestRaiseOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2b));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2b()' invoked Raise while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestGotoOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2c));
            });

            string bugReport = "Machine 'Microsoft.PSharp.TestingServices.Tests.Unit.OnHaltTest+M2c()' invoked Goto while halted.";
            AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestAPIsOnHalt()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M4));
                r.CreateMachine(typeof(M3), new E(m));
            });

            AssertSucceeded(test);
        }
    }
}
