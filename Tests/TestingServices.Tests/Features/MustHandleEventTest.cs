// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MustHandleEventTest : BaseTest
    {
        public MustHandleEventTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public MachineId Id;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
        }
        class E1 : Event { }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new Halt());
                this.Send(this.Id, new Halt());
            }
        }

        class M3 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {

            }
        }

        class M4 : Machine
        {
            [Start]
            [DeferEvents(typeof(E))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {

            }
        }

        class M5 : Machine
        {
            [Start]
            [DeferEvents(typeof(E), typeof(Halt))]
            [OnEventGotoState(typeof(E1), typeof(Next))]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            class Next : MachineState { }

            void InitOnEntry()
            {

            }
        }

        [Fact]
        public void TestMustHandleFail1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new E(), new SendOptions { MustHandle = true });
            });

            var config = Configuration.Create();

            string bugReport1 = "A must-handle event 'E' was sent to the halted machine 'M1()'.\n";
            string bugReport2 = "Machine 'M1()' halted before dequeueing must-handle event 'E'.\n";
            var expectedFunc = new Func<HashSet<string>, bool>(bugReports =>
            {
                foreach (var report in bugReports)
                {
                    if (report != bugReport1 && report != bugReport2)
                    {
                        return false;
                    }
                }
                return true;
            });

            AssertFailed(config, test, 1, expectedFunc, true);
        }

        [Fact]
        public void TestMustHandleFail2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
                r.SendEvent(m, new E(), new SendOptions { MustHandle = true });
            });

            var config = Configuration.Create().WithNumberOfIterations(100);

            string bugReport1 = "A must-handle event 'E' was sent to the halted machine 'M2()'.\n";
            string bugReport2 = "Machine 'M2()' halted before dequeueing must-handle event 'E'.\n";
            var expectedFunc = new Func<HashSet<string>, bool>(bugReports =>
            {
                foreach (var report in bugReports)
                {
                    if (report != bugReport1 && report != bugReport2)
                    {
                        return false;
                    }
                }
                return true;
            });

            AssertFailed(config, test, 1, expectedFunc, true);
        }

        [Fact]
        public void TestMustHandleFail3()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M5));
                r.SendEvent(m, new Halt());
                r.SendEvent(m, new E(), new SendOptions { MustHandle = true });
                r.SendEvent(m, new E1());
            });

            var config = Configuration.Create().WithNumberOfIterations(1);

            string bugReport = "Machine 'M5()' halted before dequeueing must-handle event 'E'.\n";

            AssertFailed(config, test, 1, new HashSet<string> { bugReport }, true);
        }

        [Fact]
        public void TestMustHandleSuccess()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new E(), new SendOptions { MustHandle = true });
                r.SendEvent(m, new Halt());
            });

            var config = Configuration.Create().WithNumberOfIterations(100);
            AssertSucceeded(config, test);
        }

        [Fact]
        public void TestMustHandleDeferFail()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E(), new SendOptions { MustHandle = true });
                r.SendEvent(m, new Halt());
            });

            var config = Configuration.Create().WithNumberOfIterations(1);
            AssertFailed(config, test, 1, true);
        }

    }
}
