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
    public class MachineTaskRunTest : BaseTest
    {
        public MachineTaskRunTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public volatile int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelMachineTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(() =>
            {
                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousMachineTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.CompletedTask;
                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousMachineTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.Delay(1);
                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousMachineTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 3;
                });

                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedParallelAsynchronousMachineTask()
        {
            SharedEntry entry = new SharedEntry();
            await MachineTask.Run(async () =>
            {
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1);
                    entry.Value = 3;
                });

                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelMachineTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(() =>
            {
                entry.Value = 5;
                return entry.Value;
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousMachineTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                await MachineTask.CompletedTask;
                entry.Value = 5;
                return entry.Value;
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousMachineTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                await MachineTask.Delay(1);
                entry.Value = 5;
                return entry.Value;
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousMachineTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                return await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                });
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelAsynchronousMachineTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await MachineTask.Run(async () =>
            {
                return await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                });
            });

            Assert.Equal(5, value);
        }
    }
}
