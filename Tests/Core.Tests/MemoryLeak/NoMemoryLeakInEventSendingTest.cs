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
    public class NoMemoryLeakInEventSendingTest : BaseTest
    {
        public NoMemoryLeakInEventSendingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        internal class E : Event
        {
            public MachineId Id;
            private int[] LargeArray;

            public E(MachineId id)
                : base()
            {
                this.Id = id;
                this.LargeArray = new int[10000000];
            }
        }

        internal class Unit : Event
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;

                try
                {
                    int counter = 0;
                    var n = this.CreateMachine(typeof(N));

                    while (counter < 1000)
                    {
                        this.Send(n, new E(this.Id));
                        await this.Receive(typeof(E));
                        counter++;
                    }
                }
                finally
                {
                    tcs.SetResult(true);
                }

                tcs.SetResult(true);
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
            }
        }

        [Fact]
        public void TestNoMemoryLeakInEventSending()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<PSharpRuntime>((r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M), new Configure(tcs));
                tcs.Task.Wait();
                r.Stop();
            });

            this.Run(config, test);
        }
    }
}
