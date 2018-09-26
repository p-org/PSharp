// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest5 
    {
        class Conf : Event
        {
            public TaskCompletionSource<bool> tcs;

            public Conf(TaskCompletionSource<bool> tcs)
            {
                this.tcs = tcs;
            }
        }

        class E : Event
        {
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Conf).tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            class Init : MachineState { }

            void HandleE()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Monitor<SafetyMonitor>(new M_Halts());
            }
        }

        class M_Halts : Event { }
        class SE_Returns : Event { }

        class SafetyMonitor : Monitor
        {
            bool M_halted = false;
            bool SE_returned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(M_Halts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SE_Returns), nameof(OnSEReturns))]
            class Init : MonitorState { }

            [Cold]
            class Done : MonitorState { }

            void OnMHalts()
            {
                this.Assert(SE_returned == false);
                M_halted = true;
            }

            void OnSEReturns()
            {
                this.Assert(M_halted);
                SE_returned = true;
                this.Goto<Done>();
            }
        }

        [Fact]
        public void TestMachineHaltsOnSendExec()
        {
            var config = Configuration.Create();
            config.EnableMonitorsInProduction = true;

            var runtime = PSharpRuntime.Create(config);
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(Harness), new Conf(tcs));
            tcs.Task.Wait();

            Assert.False(failed);
        }

    }
}
