// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest4 
    {
        class Conf : Event
        {
            public TaskCompletionSource<bool> tcs;

            public Conf(TaskCompletionSource<bool> tcs)
            {
                this.tcs = tcs;
            }
        }

        class LE : Event { }

        class E : Event
        {
            public MachineId mid;

            public E(MachineId mid)
            {
                this.mid = mid;
            }
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Conf).tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M), new E(this.Id));
                var handled = await this.Runtime.SendEventAndExecute(m, new LE());
                this.Assert(handled);
                tcs.SetResult(true);
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E).mid;
                var handled = await this.Id.Runtime.SendEventAndExecute(creator, new LE());
                this.Assert(!handled);
            }

        }


        [Fact]
        public void TestSendCycleDoesNotDeadlock()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };

            runtime.CreateMachine(typeof(Harness), new Conf(tcs));
            tcs.Task.Wait();

            Assert.False(failed);
        }

    }
}
