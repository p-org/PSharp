// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class TaskSafetyMonitorTest : BaseTest
    {
        public TaskSafetyMonitorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Notify : Event
        {
        }

        private class SafetyMonitor : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(Notify), nameof(HandleNotify))]
            private class Init : MonitorState
            {
            }

            private void HandleNotify()
            {
                this.Assert(false, "Bug found!");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async MachineTask WriteAsync()
                {
                    await MachineTask.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async MachineTask WriteWithDelayAsync()
                {
                    await MachineTask.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await MachineTask.Run(() =>
                {
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await MachineTask.Run(async () =>
                {
                    await MachineTask.Run(async () =>
                    {
                        await MachineTask.CompletedTask;
                        Specification.Monitor<SafetyMonitor>(new Notify());
                    });
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Bug found!",
            replay: true);
        }
    }
}
