// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskRunConfigureAwaitFalseTest : BaseTest
    {
        public TaskRunConfigureAwaitFalseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(() =>
                {
                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(() =>
                {
                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        entry.Value = 3;
                    }).ConfigureAwait(false);

                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        entry.Value = 5;
                    }).ConfigureAwait(false);

                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.Delay(1).ConfigureAwait(false);
                        entry.Value = 3;
                    }).ConfigureAwait(false);

                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.Delay(1).ConfigureAwait(false);
                        entry.Value = 5;
                    }).ConfigureAwait(false);

                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(() =>
                {
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    return await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        entry.Value = 5;
                        return entry.Value;
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    return await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        entry.Value = 3;
                        return entry.Value;
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelAsynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    return await MachineTask.Run(async () =>
                    {
                        await MachineTask.Delay(1).ConfigureAwait(false);
                        entry.Value = 5;
                        return entry.Value;
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelAsynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    return await MachineTask.Run(async () =>
                    {
                        await MachineTask.Delay(1).ConfigureAwait(false);
                        entry.Value = 3;
                        return entry.Value;
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
