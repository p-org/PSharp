// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class SendAndExecuteTest5 : BaseTest
    {
        public SendAndExecuteTest5(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
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
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            private class Init : MachineState
            {
            }

            private void HandleE()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Monitor<SafetyMonitor>(new M_Halts());
            }
        }

        private class M_Halts : Event
        {
        }

        private class SE_Returns : Event
        {
        }

        private class SafetyMonitor : Monitor
        {
            private bool MHalted = false;
            private bool SEReturned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(M_Halts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SE_Returns), nameof(OnSEReturns))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Done : MonitorState
            {
            }

            private void OnMHalts()
            {
                this.Assert(this.SEReturned == false);
                this.MHalted = true;
            }

            private void OnSEReturns()
            {
                this.Assert(this.MHalted);
                this.SEReturned = true;
                this.Goto<Done>();
            }
        }

        [Fact(Timeout=5000)]
        public void TestMachineHaltsOnSendExec()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(Harness));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            this.AssertSucceeded(config, test);
        }
    }
}
