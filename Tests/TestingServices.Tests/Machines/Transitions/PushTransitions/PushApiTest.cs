// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class PushApiTest : BaseTest
    {
        public PushApiTest(ITestOutputHelper output)
            : base(output)
        { }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            class Done : MachineState { }

            void EntryDone()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }
        }

        class E : Event { }

        class M2 : Machine
        {
            int cnt = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Assert(cnt == 0); // called once
                cnt++;

                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            class Done : MachineState { }

            void EntryDone()
            {
                this.Pop();
            }
        }

        class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Push<Done>();
            }

            void ExitInit()
            {
                // This assert is not reachable.
                this.Assert(false, "Bug found.");
            }

            class Done : MachineState { }
        }

        class M4a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                // Added a different failure mode here; try to Goto a state from another machine.
                this.Push<M4b.Done>();
            }

            class Done : MachineState { }
        }

        class M4b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
            }

            internal class Done : MachineState { }
        }


        [Fact]
        public void TestPushSimple()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            base.AssertFailed(test, 1, true);
        }

        [Fact]
        public void TestPushPopSimple()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestPushStateExit()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            base.AssertSucceeded(test);
        }


        [Fact]
        public void TestPushBadStateFail()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M4a));
            });

            base.AssertFailed(test, 1, true);
        }
    }
}
