// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class DeferIgnoreEventTest : BaseTest
    {
        public DeferIgnoreEventTest(ITestOutputHelper output)
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

        private class Unit : Event
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class M1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private bool boolResult;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(State))]
            [IgnoreEvents(typeof(E1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(StateEntry))]
            [IgnoreEvents(typeof(E1))]
            [OnEventDoAction(typeof(Unit), nameof(Action1))]
            [OnEventDoAction(typeof(E2), nameof(Action2))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class State : MachineState
            {
            }

            private void StateEntry()
            {
                this.Raise(new Unit());
                // this.Send(this.Id, new E1(), new SendOptions(assert: 1));
            }

            private async Task Action1()
            {
                this.Send(this.Id, new E1(), new SendOptions(assert: 1));
                this.Send(this.Id, new E2(), new SendOptions(assert: 1));
                this.Send(this.Id, new E3(), new SendOptions(assert: 1));
                Event receivedEvent = await this.Receive(typeof(E3));
                if (receivedEvent is E3 evtE3)
                {
                    this.boolResult = true;
                }
            }

            private void Action2()
            {
                if (this.boolResult)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Tcs.SetResult(false);
                }
            }

            [OnEntry(nameof(S2Entry))]
            private class S2 : MachineState
            {
            }

            private void S2Entry()
            {
                // Unreachable:
                this.Tcs.SetResult(false);
            }
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Integration\DynamicError\SEM_OneMachine_28\DeferIgnore2.p
        public void DeferIgnoreEvent()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }
    }
}
