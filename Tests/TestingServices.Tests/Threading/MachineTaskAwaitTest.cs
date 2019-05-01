// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MachineTaskAwaitTest : BaseTest
    {
        public MachineTaskAwaitTest(ITestOutputHelper output)
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
        public void TestAwaitSynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        private static async MachineTask NestedWriteAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            await WriteAsync(entry, value);
        }

        private static async MachineTask NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            await WriteWithDelayAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousMachineTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousMachineTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 3);
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
            await MachineTask.Delay(1);
            entry.Value = value;
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultAsync(entry, 5);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultAsync(entry, 3);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultWithDelayAsync(entry, 5);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultWithDelayAsync(entry, 3);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        private static async MachineTask<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            return await GetWriteResultAsync(entry, value);
        }

        private static async MachineTask<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            return await GetWriteResultWithDelayAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 5);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 3);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousMachineTaskResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 5);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousMachineTaskResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 3);
                Specification.Assert(value == 5, "Value is '{0}' instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
