// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskConfigureAwaitTrueTest : BaseTest
    {
        public TaskConfigureAwaitTrueTest(ITestOutputHelper output)
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
            await MachineTask.Delay(1).ConfigureAwait(true);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        private static async MachineTask NestedWriteAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            await WriteAsync(entry, value).ConfigureAwait(true);
        }

        private static async MachineTask NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1).ConfigureAwait(true);
            await WriteWithDelayAsync(entry, value).ConfigureAwait(true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        private static async MachineTask<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            entry.Value = value;
            return entry.Value;
        }

        private static async MachineTask<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1).ConfigureAwait(true);
            entry.Value = value;
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultWithDelayAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        private static async MachineTask<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            return await GetWriteResultAsync(entry, value).ConfigureAwait(true);
        }

        private static async MachineTask<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1).ConfigureAwait(true);
            return await GetWriteResultWithDelayAsync(entry, value).ConfigureAwait(true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 3).ConfigureAwait(true);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
