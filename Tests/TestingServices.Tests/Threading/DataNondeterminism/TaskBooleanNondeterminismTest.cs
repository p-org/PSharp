// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskBooleanNondeterminismTest : BaseTest
    {
        public TaskBooleanNondeterminismTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public void TestBooleanNondeterminismInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async MachineTask WriteAsync()
                {
                    await MachineTask.CompletedTask;
                    if (Specification.ChooseRandomBoolean())
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
        public void TestBooleanNondeterminismInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async MachineTask WriteWithDelayAsync()
                {
                    await MachineTask.Delay(1);
                    if (Specification.ChooseRandomBoolean())
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
        public void TestBooleanNondeterminismInParallelTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(() =>
                {
                    if (Specification.ChooseRandomBoolean())
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
        public void TestBooleanNondeterminismInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    if (Specification.ChooseRandomBoolean())
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
        public void TestBooleanNondeterminismInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1);
                    if (Specification.ChooseRandomBoolean())
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
        public void TestBooleanNondeterminismInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        if (Specification.ChooseRandomBoolean())
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
