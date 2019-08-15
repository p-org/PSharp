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
    public class MachineTaskWaitAnyTest : BaseTest
    {
        public MachineTaskWaitAnyTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        private static async MachineTask WriteAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            entry.Value = value;
        }

        private static async MachineTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoSynchronousMachineTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask task1 = WriteAsync(entry, 5);
                MachineTask task2 = WriteAsync(entry, 3);
                int index = MachineTask.WaitAny(task1, task2);
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Both task have completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoAsynchronousMachineTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask task1 = WriteWithDelayAsync(entry, 3);
                MachineTask task2 = WriteWithDelayAsync(entry, 5);
                int index = MachineTask.WaitAny(task1, task2);
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelMachineTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task1 = MachineTask.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                MachineTask task2 = MachineTask.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                int index = MachineTask.WaitAny(task1, task2);

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "One task has not completed.",
            replay: true);
        }

        private static async MachineTask<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            entry.Value = value;
            await MachineTask.CompletedTask;
            return entry.Value;
        }

        private static async MachineTask<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            entry.Value = value;
            await MachineTask.Delay(1);
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoSynchronousMachineTaskResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask<int> task1 = GetWriteResultAsync(entry, 5);
                MachineTask<int> task2 = GetWriteResultAsync(entry, 3);
                int index = MachineTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Both task have completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoAsynchronousMachineTaskResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                MachineTask<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                int index = MachineTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelSynchronousMachineTaskResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask<int> task1 = MachineTask.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 5);
                });

                MachineTask<int> task2 = MachineTask.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 3);
                });

                int index = MachineTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelAsynchronousMachineTaskResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask<int> task1 = MachineTask.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 5);
                });

                MachineTask<int> task2 = MachineTask.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 3);
                });

                int index = MachineTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "One task has not completed.",
            replay: true);
        }
    }
}
