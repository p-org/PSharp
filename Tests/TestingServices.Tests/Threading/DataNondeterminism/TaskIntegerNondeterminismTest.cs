// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskIntegerNondeterminismTest : BaseTest
    {
        public TaskIntegerNondeterminismTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async MachineTask WriteAsync()
                {
                    await MachineTask.CompletedTask;
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                }

                await WriteAsync();
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async MachineTask WriteWithDelayAsync()
                {
                    await MachineTask.Delay(1);
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                }

                await WriteWithDelayAsync();
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInParallelTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(() =>
                {
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1);
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        if (Specification.ChooseRandomInteger(5) == 0)
                        {
                            entry.Value = 3;
                        }
                        else
                        {
                            entry.Value = 5;
                        }
                    });
                });

                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }
    }
}
