// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskWhenAllTest : BaseTest
    {
        public TaskWhenAllTest(ITestOutputHelper output)
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
        public void TestWhenAllWithTwoSynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask task1 = WriteAsync(entry, 5);
                MachineTask task2 = WriteAsync(entry, 3);
                await MachineTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask task1 = WriteWithDelayAsync(entry, 3);
                MachineTask task2 = WriteWithDelayAsync(entry, 5);
                await MachineTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelTasks()
        {
            this.TestWithError(async () =>
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

                await MachineTask.WhenAll(task1, task2);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
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
        public void TestWhenAllWithTwoSynchronousTaskResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask<int> task1 = GetWriteResultAsync(entry, 5);
                MachineTask<int> task2 = GetWriteResultAsync(entry, 3);
                int[] results = await MachineTask.WhenAll(task1, task2);
                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
                Specification.Assert(results[0] == results[1], "Results are not equal.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTaskResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                MachineTask<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                MachineTask<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                int[] results = await MachineTask.WhenAll(task1, task2);
                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelSynchronousTaskResults()
        {
            this.TestWithError(async () =>
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

                int[] results = await MachineTask.WhenAll(task1, task2);

                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
                Specification.Assert(results[0] == results[1], "Results are not equal.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelAsynchronousTaskResults()
        {
            this.TestWithError(async () =>
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

                int[] results = await MachineTask.WhenAll(task1, task2);

                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Found unexpected value.",
            replay: true);
        }
    }
}
