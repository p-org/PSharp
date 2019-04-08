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
        {
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
                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            private class Done : MachineState
            {
            }

            private void EntryDone()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }
        }

        private class E : Event
        {
        }

        private class M2 : Machine
        {
            private int cnt = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Assert(this.cnt == 0); // called once
                this.cnt++;

                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            private class Done : MachineState
            {
            }

            private void EntryDone()
            {
                this.Pop();
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Push<Done>();
            }

            private void ExitInit()
            {
                // This assert is not reachable.
                this.Assert(false, "Bug found.");
            }

            private class Done : MachineState
            {
            }
        }

        private class M4a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                // Added a different failure mode here; try to Goto a state from another machine.
                this.Push<M4b.Done>();
            }

            private class Done : MachineState
            {
            }
        }

        private class M4b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            internal class Done : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestPushSimple()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            this.AssertFailed(test, 1, true);
        }

        [Fact(Timeout=5000)]
        public void TestPushPopSimple()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestPushStateExit()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestPushBadStateFail()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M4a));
            });

            this.AssertFailed(test, 1, true);
        }
    }
}
