// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MachineTaskRunConfigureAwaitTrueTest : BaseTest
    {
        public MachineTaskRunConfigureAwaitTrueTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(() =>
                {
                    entry.Value = 5;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(() =>
                {
                    entry.Value = 3;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 5;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 3;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(true);
                    entry.Value = 5;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(true);
                    entry.Value = 3;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousMachineTask()
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
                    }).ConfigureAwait(true);

                    entry.Value = 5;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelSynchronousMachineTaskFailure()
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
                    }).ConfigureAwait(true);

                    entry.Value = 3;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelAsynchronousMachineTask()
        {
            this.Test(async () =>
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

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelAsynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.Delay(1).ConfigureAwait(true);
                        entry.Value = 5;
                    }).ConfigureAwait(true);

                    entry.Value = 3;
                }).ConfigureAwait(true);

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(() =>
                {
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(true);
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1).ConfigureAwait(true);
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousMachineTaskResult()
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
                    }).ConfigureAwait(true);
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousMachineTaskResultFailure()
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
                    }).ConfigureAwait(true);
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelAsynchronousMachineTaskResult()
        {
            this.Test(async () =>
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

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelAsynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await MachineTask.Run(async () =>
                {
                    return await MachineTask.Run(async () =>
                    {
                        await MachineTask.Delay(1).ConfigureAwait(true);
                        entry.Value = 3;
                        return entry.Value;
                    }).ConfigureAwait(true);
                }).ConfigureAwait(true);

                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
