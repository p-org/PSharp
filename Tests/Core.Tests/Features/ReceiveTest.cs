﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class ReceiveTest : BaseTest
    {
        public ReceiveTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
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

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E1());
                await this.Receive(typeof(E1));
                tcs.SetResult(true);
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E1());
                await this.Receive(typeof(E1), e => e is E1);
                tcs.SetResult(true);
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E1());
                await this.Receive(typeof(E1), typeof(E2));
                tcs.SetResult(true);
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var mid = this.CreateMachine(typeof(M5), new E2(this.Id));
                this.Send(mid, new E2(this.Id));
                await this.Receive(typeof(E2));
                this.Send(mid, new E2(this.Id));
                this.Send(mid, new E2(this.Id));
                await this.Receive(typeof(E2));
                tcs.SetResult(true);
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(Handle))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var mid = (this.ReceivedEvent as E2).Id;
                var e = (E2)await this.Receive(typeof(E2));
                this.Send(e.Id, new E2(this.Id));
            }

            private async Task Handle()
            {
                var mid = (this.ReceivedEvent as E2).Id;
                var e = (E2)await this.Receive(typeof(E2));
                this.Send(e.Id, new E2(this.Id));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventOneMachine()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventWithPredicateOneMachine()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M2), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventMultipleTypesOneMachine()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M3), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventTwoMachines()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M4), new SetupEvent(tcs));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }
    }
}
