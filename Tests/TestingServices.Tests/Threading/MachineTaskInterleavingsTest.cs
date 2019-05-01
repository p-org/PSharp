// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MachineTaskInterleavingsTest : BaseTest
    {
        public MachineTaskInterleavingsTest(ITestOutputHelper output)
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
        public void TestInterleavingsWithOneSynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task = WriteAsync(entry, 3);
                entry.Value = 5;
                await task;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneAsynchronousMachineTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task = WriteWithDelayAsync(entry, 3);
                entry.Value = 5;
                await task;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneParallelMachineTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task = MachineTask.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                await WriteAsync(entry, 5);
                await task;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoSynchronousMachineTasks()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task1 = WriteAsync(entry, 3);
                MachineTask task2 = WriteAsync(entry, 5);

                await task1;
                await task2;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoAsynchronousMachineTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task1 = WriteWithDelayAsync(entry, 3);
                MachineTask task2 = WriteWithDelayAsync(entry, 5);

                await task1;
                await task2;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoParallelMachineTasks()
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

                await task1;
                await task2;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithNestedParallelMachineTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask task1 = MachineTask.Run(async () =>
                {
                    MachineTask task2 = MachineTask.Run(async () =>
                    {
                        await WriteAsync(entry, 5);
                    });

                    await WriteAsync(entry, 3);
                    await task2;
                });

                await task1;

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
