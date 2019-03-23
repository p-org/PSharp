// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class SendAndExecuteTest6 : BaseTest
    {
        public SendAndExecuteTest6(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class Config : Event
        {
            public bool HandleException;

            public Config(bool handleEx)
            {
                this.HandleException = handleEx;
            }
        }

        private class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M), this.ReceivedEvent);
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(false);
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M : Machine
        {
            private bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.HandleException = (this.ReceivedEvent as Config).HandleException;
            }

            private void HandleE()
            {
                throw new Exception();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return this.HandleException ? OnExceptionOutcome.HandledException : OnExceptionOutcome.ThrowException;
            }
        }

        private class SE_Returns : Event
        {
        }

        private class SafetyMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(SE_Returns), typeof(Done))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Done : MonitorState
            {
            }
        }

        [Fact]
        public void TestHandledExceptionOnSendExec()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(Harness), new Config(true));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            this.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestUnHandledExceptionOnSendExec()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(Harness), new Config(false));
            });

            var config = Configuration.Create();
            this.AssertFailed(config, test, 1, bugReports =>
            {
                foreach (var report in bugReports)
                {
                    if (!report.StartsWith("Exception 'System.Exception' was thrown in machine 'M()'"))
                    {
                        return false;
                    }
                }
                return true;
            }, true);
        }
    }
}
