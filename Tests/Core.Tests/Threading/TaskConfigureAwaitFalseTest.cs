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
    public class TaskConfigureAwaitFalseTest : BaseTest
    {
        public TaskConfigureAwaitFalseTest(ITestOutputHelper output)
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
            await MachineTask.Delay(1).ConfigureAwait(false);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await WriteAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await WriteWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        private static async MachineTask NestedWriteAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            await WriteAsync(entry, value).ConfigureAwait(false);
        }

        private static async MachineTask NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1).ConfigureAwait(false);
            await WriteWithDelayAsync(entry, value).ConfigureAwait(false);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await NestedWriteAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await NestedWriteWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        private static async MachineTask<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            entry.Value = value;
            return entry.Value;
        }

        private static async MachineTask<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1).ConfigureAwait(false);
            entry.Value = value;
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await GetWriteResultAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await GetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }

        private static async MachineTask<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            return await GetWriteResultAsync(entry, value).ConfigureAwait(false);
        }

        private static async MachineTask<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            return await GetWriteResultWithDelayAsync(entry, value).ConfigureAwait(false);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await NestedGetWriteResultAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await NestedGetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }
    }
}
