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
    public class ReceiveTests1 : BaseTest
    {
        public ReceiveTests1(ITestOutputHelper output)
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

        private class E : Event
        {
        }

        private class F : Event
        {
        }

        private class Unit : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Unit(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
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
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var bid = this.CreateMachine(typeof(B), new SetupEvent(tcs));
                this.Send(bid, new F());
                // this.Assert(false);
                tcs.SetResult(true);
            }
        }

        private class B : Machine
        {
            // TODO: Why error?
            // private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(X))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Y))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryY))]
            private class Y : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                // this.Assert(false);
                this.Raise(new Unit(tcs));
                // var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
            }

            private async Task InitOnEntryX()
            {
                this.Assert(false);
                var tcs = (this.ReceivedEvent as Unit).Tcs;
                await this.Receive(typeof(E));
                // tcs.SetResult(true);
            }

            private void InitOnEntryY()
            {
                // Since Receive in state X is blocking, event F
                // will never get dequeued, and this handler is unreachable:
                this.Assert(false);
            }
        }

        // private class M3 : Machine
        // {
        //    [Start]
        //    [OnEntry(nameof(InitOnEntry))]
        //    private class Init : MachineState
        //    {
        //    }

        // private async Task InitOnEntry()
        //    {
        //        var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
        //        this.Send(this.Id, new E1());
        //        await this.Receive(typeof(E1), typeof(E2));
        //        tcs.SetResult(true);
        //    }
        // }

        // private class M4 : Machine
        // {
        //    [Start]
        //    [OnEntry(nameof(InitOnEntry))]
        //    private class Init : MachineState
        //    {
        //    }

        // private async Task InitOnEntry()
        //    {
        //        var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
        //        var mid = this.CreateMachine(typeof(M5), new E2(this.Id));
        //        this.Send(mid, new E2(this.Id));
        //        await this.Receive(typeof(E2));
        //        this.Send(mid, new E2(this.Id));
        //        this.Send(mid, new E2(this.Id));
        //        await this.Receive(typeof(E2));
        //        tcs.SetResult(true);
        //    }
        // }

        // private class M5 : Machine
        // {
        //    [Start]
        //    [OnEntry(nameof(InitOnEntry))]
        //    [OnEventDoAction(typeof(E2), nameof(Handle))]
        //    private class Init : MachineState
        //    {
        //    }

        // private async Task InitOnEntry()
        //    {
        //        var mid = (this.ReceivedEvent as E2).Id;
        //        var e = (E2)await this.Receive(typeof(E2));
        //        this.Send(e.Id, new E2(this.Id));
        //    }

        // private async Task Handle()
        //    {
        //        var mid = (this.ReceivedEvent as E2).Id;
        //        var e = (E2)await this.Receive(typeof(E2));
        //        this.Send(e.Id, new E2(this.Id));
        //    }
        // }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventBlocking()
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
    }
}
