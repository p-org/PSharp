// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskYieldTest : BaseTest
    {
        public TaskYieldTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTaskYield()
        {
            this.Test(async () =>
            {
                await MachineTask.Yield();
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousTaskYield()
        {
            this.Test(async () =>
            {
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Yield();
                });

                await MachineTask.Yield();
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskYield()
        {
            this.Test(async () =>
            {
                MachineTask task = MachineTask.Run(async () =>
                {
                    await MachineTask.Yield();
                });

                await MachineTask.Yield();
                await task;
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksYield()
        {
            this.Test(async () =>
            {
                MachineTask task1 = MachineTask.Run(async () =>
                {
                    await MachineTask.Yield();
                });

                MachineTask task2 = MachineTask.Run(async () =>
                {
                    await MachineTask.Yield();
                });

                await MachineTask.Yield();
                await MachineTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksWriteWithYield()
        {
            this.Test(async () =>
            {
                int entry = 0;

                async MachineTask WriteAsync(int value)
                {
                    await MachineTask.Yield();
                    entry = value;
                    Specification.Assert(entry == value, "Value is '{0}' instead of '{1}'.", entry, value);
                }

                MachineTask task1 = MachineTask.Run(async () =>
                {
                    await WriteAsync(3);
                });

                MachineTask task2 = MachineTask.Run(async () =>
                {
                    await WriteAsync(5);
                });

                await MachineTask.Yield();
                await MachineTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksWriteWithYieldFail()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;

                async MachineTask WriteAsync(int value)
                {
                    entry = value;
                    await MachineTask.Yield();
                    Specification.Assert(entry == value, "Found unexpected value '{0}' after write.", entry);
                }

                MachineTask task1 = WriteAsync(3);
                MachineTask task2 = WriteAsync(5);
                await MachineTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Found unexpected value '' after write.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTwoAsynchronousTasksWriteWithYieldFail()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;

                async MachineTask WriteAsync(int value)
                {
                    await MachineTask.Yield();
                    entry = value;
                }

                MachineTask task1 = WriteAsync(3);
                MachineTask task2 = WriteAsync(5);
                await MachineTask.WhenAll(task1, task2);
                Specification.Assert(entry == 5, "Value is '{0}' instead of 5.", entry);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
