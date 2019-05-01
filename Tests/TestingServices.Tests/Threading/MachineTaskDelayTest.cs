// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class MachineTaskDelayTest : BaseTest
    {
        public MachineTaskDelayTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        private static async MachineTask WriteWithDelayAsync(SharedEntry entry, int value, int delay)
        {
            for (int i = 0; i < 2; i++)
            {
                entry.Value = value + i;
                await MachineTask.Delay(delay);
            }
        }

        [Fact(Timeout=5000)]
        public void TestInterleavingsInLoopWithSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask[] tasks = new MachineTask[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithDelayAsync(entry, i, 0);
                }

                await MachineTask.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is '{0}' instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLoopWithAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                MachineTask[] tasks = new MachineTask[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithDelayAsync(entry, i, 1);
                }

                await MachineTask.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is '{0}' instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 2.",
            replay: true);
        }
    }
}
