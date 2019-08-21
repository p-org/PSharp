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
    public class TaskRunConfigureAwaitTrueTest : BaseTest
    {
        public TaskRunConfigureAwaitTrueTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public volatile int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(() =>
            {
                entry.Value = 5;
            }).ConfigureAwait(true);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.CompletedTask;
                entry.Value = 5;
            }).ConfigureAwait(true);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.Delay(1).ConfigureAwait(true);
                entry.Value = 5;
            }).ConfigureAwait(true);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 3;
                }).ConfigureAwait(true);

                entry.Value = 5;
            }).ConfigureAwait(true);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedParallelAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(true);
                    entry.Value = 3;
                }).ConfigureAwait(true);

                entry.Value = 5;
            }).ConfigureAwait(true);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(() =>
            {
                entry.Value = 5;
                return entry.Value;
            }).ConfigureAwait(true);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                await MachineTask.CompletedTask;
                entry.Value = 5;
                return entry.Value;
            }).ConfigureAwait(true);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                await MachineTask.Delay(1).ConfigureAwait(true);
                entry.Value = 5;
                return entry.Value;
            }).ConfigureAwait(true);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                return await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(true);
            }).ConfigureAwait(true);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                return await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(true);
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(true);
            }).ConfigureAwait(true);

            Assert.Equal(5, value);
        }
    }
}
