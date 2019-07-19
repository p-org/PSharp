// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class SafeStackTest : BaseTest
    {
        public SafeStackTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private struct SafeStackItem<T>
        {
            public T Value;
            public volatile int Next;
        }

        private class SafeStack<T>
        {
            public readonly SafeStackItem<T>[] Array;
            private volatile int Head;
            private volatile int Count;

            private readonly MachineLock ArrayLock;
            private readonly MachineLock HeadLock;
            private readonly MachineLock CountLock;

            public SafeStack(int pushCount)
            {
                this.Array = new SafeStackItem<T>[pushCount];
                this.Head = 0;
                this.Count = pushCount;

                for (int i = 0; i < pushCount - 1; i++)
                {
                    this.Array[i].Next = i + 1;
                }

                this.Array[pushCount - 1].Next = -1;

                this.ArrayLock = MachineLock.Create();
                this.HeadLock = MachineLock.Create();
                this.CountLock = MachineLock.Create();
            }

            public async MachineTask PushAsync(int index)
            {
                // Console.WriteLine($"\nTask {Task.CurrentId} starts push {index}.\n");
                Specification.InjectContextSwitch();
                int head = this.Head;
                // Console.WriteLine($"\nTask {Task.CurrentId} reads head {head} in push {index}.\n");
                bool compareExchangeResult = false;

                do
                {
                    Specification.InjectContextSwitch();
                    this.Array[index].Next = head;
                    // Console.WriteLine($"\nTask {Task.CurrentId} sets [{index}].next to {head} during push.\n");

                    Specification.InjectContextSwitch();
                    using (await this.HeadLock.AcquireAsync())
                    {
                        if (this.Head == head)
                        {
                            this.Head = index;
                            compareExchangeResult = true;
                            // Console.WriteLine($"\nTask {Task.CurrentId} compare-exchange in push {index} succeeded (head = {this.Head}, count = {this.Count}).\n");
                        }
                        else
                        {
                            head = this.Head;
                            // Console.WriteLine($"\nTask {Task.CurrentId} compare-exchange in push {index} failed and re-read head {head}.\n");
                        }
                    }
                }
                while (!compareExchangeResult);

                Specification.InjectContextSwitch();
                using (await this.CountLock.AcquireAsync())
                {
                    this.Count++;
                }

                // Console.WriteLine($"\nTask {Task.CurrentId} pushed {index} (head = {this.Head}, count = {this.Count}).");
                // Console.WriteLine($"   [0] = {this.Array[0]} | next = {this.Array[0].Next}");
                // Console.WriteLine($"   [1] = {this.Array[1]} | next = {this.Array[1].Next}");
                // Console.WriteLine($"   [2] = {this.Array[2]} | next = {this.Array[2].Next}");
                // Console.WriteLine($"\n");
            }

            public async MachineTask<int> PopAsync()
            {
                // Console.WriteLine($"\nTask {Task.CurrentId} starts pop.\n");
                while (this.Count > 1)
                {
                    Specification.InjectContextSwitch();
                    int head = this.Head;
                    // Console.WriteLine($"\nTask {Task.CurrentId} reads head {head} in pop ([{head}].next is {this.Array[head].Next}).\n");

                    int next;
                    Specification.InjectContextSwitch();
                    using (await this.ArrayLock.AcquireAsync())
                    {
                        next = this.Array[head].Next;
                        this.Array[head].Next = -1;
                        // Console.WriteLine($"\nTask {Task.CurrentId} exchanges {next} from [{head}].next with -1.\n");
                    }

                    Specification.InjectContextSwitch();
                    int headTemp = head;

                    bool compareExchangeResult = false;
                    Specification.InjectContextSwitch();
                    using (await this.HeadLock.AcquireAsync())
                    {
                        if (this.Head == headTemp)
                        {
                            this.Head = next;
                            compareExchangeResult = true;
                            // Console.WriteLine($"\nTask {Task.CurrentId} compare-exchange in pop succeeded (head = {this.Head}, count = {this.Count}).\n");
                        }
                        else
                        {
                            headTemp = this.Head;
                            // Console.WriteLine($"\nTask {Task.CurrentId} compare-exchange in pop failed and re-read head {headTemp}.\n");
                        }
                    }

                    if (compareExchangeResult)
                    {
                        Specification.InjectContextSwitch();
                        using (await this.CountLock.AcquireAsync())
                        {
                            this.Count--;
                        }

                        Specification.InjectContextSwitch();
                        // Console.WriteLine($"\nTask {Task.CurrentId} pops {head} (head = {this.Head}, count = {this.Count}).");
                        // Console.WriteLine($"   [0] = {this.Array[0]} | next = {this.Array[0].Next}");
                        // Console.WriteLine($"   [1] = {this.Array[1]} | next = {this.Array[1].Next}");
                        // Console.WriteLine($"   [2] = {this.Array[2]} | next = {this.Array[2].Next}");
                        // Console.WriteLine($"\n");
                        return head;
                    }
                    else
                    {
                        Specification.InjectContextSwitch();
                        using (await this.ArrayLock.AcquireAsync())
                        {
                            this.Array[head].Next = next;
                        }

                        Specification.InjectContextSwitch();
                    }
                }

                return -1;
            }
        }

        // [Fact(Timeout = 5000)]
        public void TestSafeStackFailure()
        {
            this.TestWithError(async () =>
            {
                int numTasks = 3;
                var stack = new SafeStack<int>(3);

                MachineTask[] tasks = new MachineTask[numTasks];
                for (int i = 0; i < numTasks; ++i)
                {
                    tasks[i] = MachineTask.Run(async () =>
                    {
                        // Console.WriteLine($"\nStarting task {Task.CurrentId}.\n");
                        for (int j = 0; j != 2; j += 1)
                        {
                            int elem;
                            for (; ; )
                            {
                                elem = await stack.PopAsync();
                                if (elem >= 0)
                                {
                                    break;
                                }

                                Specification.InjectContextSwitch();
                            }

                            stack.Array[elem].Value = Task.CurrentId.Value;
                            // Console.WriteLine($"\nTask {Task.CurrentId} writes value {Task.CurrentId.Value} to [{elem}].");
                            Specification.InjectContextSwitch();
                            Specification.Assert(stack.Array[elem].Value == Task.CurrentId.Value, "Assertion failed.");

                            await stack.PushAsync(elem);
                        }
                    });
                }

                await MachineTask.WhenAll(tasks);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
