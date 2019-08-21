// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskExceptionTest : BaseTest
    {
        public TaskExceptionTest(ITestOutputHelper output)
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
        public void TestNoSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteAsync(entry, 5);
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestNoAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteWithDelayAsync(entry, 5);
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = MachineTask.Run(() =>
                {
                    entry.Value = 5;
                });

                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = MachineTask.Run(async () =>
                {
                    entry.Value = 5;
                    await MachineTask.Delay(1);
                });
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelFuncTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async MachineTask func()
                {
                    entry.Value = 5;
                    await MachineTask.Delay(1);
                }

                var task = MachineTask.Run(func);
                var innerTask = await task;
                await innerTask;

                Specification.Assert(innerTask.Status == TaskStatus.RanToCompletion,
                    $"Status is '{innerTask.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        private static async MachineTask WriteWithExceptionAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            entry.Value = value;
            throw new InvalidOperationException();
        }

        private static async MachineTask WriteWithDelayedExceptionAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            entry.Value = value;
            throw new InvalidOperationException();
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteWithExceptionAsync(entry, 5);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteWithDelayedExceptionAsync(entry, 5);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = MachineTask.Run(() =>
                {
                    entry.Value = 5;
                    throw new InvalidOperationException();
                });

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = MachineTask.Run(async () =>
                {
                    entry.Value = 5;
                    await MachineTask.Delay(1);
                    throw new InvalidOperationException();
                });

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelFuncTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async MachineTask func()
                {
                    entry.Value = 5;
                    await MachineTask.Delay(1);
                    throw new InvalidOperationException();
                }

                var task = MachineTask.Run(func);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }
    }
}
