// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class NoMemoryLeakInEventSendingTest : BaseTest
    {
        public NoMemoryLeakInEventSendingTest(ITestOutputHelper output)
            : base(output)
        { }

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
            int[] LargeArray;

            public E(MachineId id)
                : base()
            {
                this.Id = id;
                this.LargeArray = new int[10000000];
            }
        }

        internal class Unit : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;

                try
                {
                    int counter = 0;
                    var n = CreateMachine(typeof(N));

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

        class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            class Init : MachineState { }

            void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
            }
        }

        [Fact]
        public void TestNoMemoryLeakInEventSending()
        {
            var tcs = new TaskCompletionSource<bool>();
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            tcs.Task.Wait();
            runtime.Stop();
        }
    }
}
