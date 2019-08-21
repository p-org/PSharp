// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskLockTest : BaseTest
    {
        public TaskLockTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestLockUnlockTask()
        {
            this.Test(async () =>
            {
                MachineLock mutex = MachineLock.Create();
                var releaser = await mutex.AcquireAsync();
                releaser.Dispose();
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestLockTwiceTask()
        {
            this.TestWithError(async () =>
            {
                MachineLock mutex = MachineLock.Create();
                await mutex.AcquireAsync();
                await mutex.AcquireAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Livelock detected. 'Microsoft.PSharp.TestingServices.Threading.TestEntryPointWorkMachine()' is waiting " +
                "to access a concurrent resource that is acquired by another task, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasks()
        {
            this.Test(async () =>
            {
                int entry = 0;
                MachineLock mutex = MachineLock.Create();

                async MachineTask WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry = value;
                    }
                }

                MachineTask task1 = WriteAsync(3);
                MachineTask task2 = WriteAsync(5);
                await MachineTask.WhenAll(task1, task2);
                Specification.Assert(entry == 5, "Value is '{0}' instead of 5.", entry);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasks()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;
                MachineLock mutex = MachineLock.Create();

                async MachineTask WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry = value;
                    }
                }

                MachineTask task1 = MachineTask.Run(async () =>
                {
                    await WriteAsync(3);
                });

                MachineTask task2 = MachineTask.Run(async () =>
                {
                    await WriteAsync(5);
                });

                await MachineTask.WhenAll(task1, task2);
                Specification.Assert(entry == 5, "Value is '{0}' instead of 5.", entry);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasksWithYield()
        {
            this.Test(async () =>
            {
                int entry = 0;
                MachineLock mutex = MachineLock.Create();

                async MachineTask WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry = value;
                        await MachineTask.Yield();
                        Specification.Assert(entry == value, "Value is '{0}' instead of '{1}'.", entry, value);
                    }
                }

                MachineTask task1 = WriteAsync(3);
                MachineTask task2 = WriteAsync(5);
                await MachineTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }
    }
}
