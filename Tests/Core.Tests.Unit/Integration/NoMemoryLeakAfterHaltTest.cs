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
    public class NoMemoryLeakAfterHaltTest : BaseTest
    {
        public NoMemoryLeakAfterHaltTest(ITestOutputHelper output)
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

            public E(MachineId id)
                : base()
            {
                this.Id = id;
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
                    while (counter < 100)
                    {
                        var n = CreateMachine(typeof(N));
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
            int[] LargeArray;

            [Start]
            [OnEntry(nameof(Configure))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            class Init : MachineState { }

            void Configure()
            {
                this.LargeArray = new int[10000000];
                this.LargeArray[this.LargeArray.Length - 1] = 1;
            }

            void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
                Raise(new Halt());
            }
        }

        [Fact]
        public void TestNoMemoryLeakAfterHalt()
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
