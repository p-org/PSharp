// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest7 
    {
        class E : Event
        {
        }

        class Config : Event
        {
            public TaskCompletionSource<bool> tcs;

            public Config(TaskCompletionSource<bool> tcs)
            {
                this.tcs = tcs;
            }
        }



        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Config).tcs;
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }

        }

        class M : Machine
        {

            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestUnhandledEventOnSendExec()
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            var message = string.Empty;

            runtime.OnFailure += delegate (Exception ex)
            {
                if (!failed)
                {
                    message = (ex is MachineActionExceptionFilterException) ? ex.InnerException.Message : ex.Message;
                    failed = true;
                    tcs.TrySetResult(false);
                }
            };
            runtime.CreateMachine(typeof(Harness), new Config(tcs));
            tcs.Task.Wait();

            Assert.True(failed);
            Assert.Equal("Machine '1(Microsoft.PSharp.Core.Tests.Unit.SendAndExecuteTest7+M)' received event 'Microsoft.PSharp.Core.Tests.Unit.SendAndExecuteTest7+E' that cannot be handled.",
                message);
        }

    }
}
