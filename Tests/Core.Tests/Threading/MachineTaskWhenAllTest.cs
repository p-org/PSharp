// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class MachineTaskWhenAllTest : BaseTest
    {
        public MachineTaskWhenAllTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public volatile int Value = 0;
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
        public async Task TestWhenAllWithTwoSynchronousMachineTasks()
        {
            SharedEntry entry = new SharedEntry();
            MachineTask task1 = WriteAsync(entry, 5);
            MachineTask task2 = WriteAsync(entry, 3);
            await MachineTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoAsynchronousMachineTasks()
        {
            SharedEntry entry = new SharedEntry();
            MachineTask task1 = WriteWithDelayAsync(entry, 3);
            MachineTask task2 = WriteWithDelayAsync(entry, 5);
            await MachineTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoParallelMachineTasks()
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

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        private static async MachineTask<int> GetWriteResultAsync(int value)
        {
            await MachineTask.CompletedTask;
            return value;
        }

        private static async MachineTask<int> GetWriteResultWithDelayAsync(int value)
        {
            await MachineTask.Delay(1);
            return value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoSynchronousMachineTaskResults()
        {
            MachineTask<int> task1 = GetWriteResultAsync(5);
            MachineTask<int> task2 = GetWriteResultAsync(3);
            int[] results = await MachineTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoAsynchronousMachineTaskResults()
        {
            MachineTask<int> task1 = GetWriteResultWithDelayAsync(5);
            MachineTask<int> task2 = GetWriteResultWithDelayAsync(3);
            int[] results = await MachineTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoParallelSynchronousMachineTaskResults()
        {
            MachineTask<int> task1 = MachineTask.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            MachineTask<int> task2 = MachineTask.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            int[] results = await MachineTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoParallelAsynchronousMachineTaskResults()
        {
            MachineTask<int> task1 = MachineTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            MachineTask<int> task2 = MachineTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            int[] results = await MachineTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }
    }
}
