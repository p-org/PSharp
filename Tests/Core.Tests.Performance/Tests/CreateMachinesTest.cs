// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    [Config(typeof(Configuration))]
    public class CreateMachinesTest
    {
        class Node : Machine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;
                public int Size;
                public int Counter;

                internal Configure(TaskCompletionSource<bool> tcs, int size)
                {
                    this.TCS = tcs;
                    this.Size = size;
                    this.Counter = 0;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;
                var size = (this.ReceivedEvent as Configure).Size;
                var counter = Interlocked.Increment(ref (this.ReceivedEvent as Configure).Counter);
                if (counter == size)
                {
                    tcs.TrySetResult(true);
                }
            }
        }

        [Params(100, 1000, 10000)]
        public int Size { get; set; }

        [Benchmark]
        public void CreateMachines()
        {
            var runtime = new StateMachineRuntime();

            var tcs = new TaskCompletionSource<bool>();
            Node.Configure evt = new Node.Configure(tcs, Size);

            for (int idx = 0; idx < Size; idx++)
            {
                runtime.CreateMachine(typeof(Node), null, evt, null);
            }

            tcs.Task.Wait();
        }

        [Benchmark(Baseline = true)]
        public void CreateTasks()
        {
            var tcs = new TaskCompletionSource<bool>();
            int counter = 0;

            for (int idx = 0; idx < Size; idx++)
            {
                var task = new Task(() => {
                    int value = Interlocked.Increment(ref counter);
                    if (value == Size)
                    {
                        tcs.TrySetResult(true);
                    }
                });

                task.Start();
            }

            tcs.Task.Wait();
        }
    }
}
