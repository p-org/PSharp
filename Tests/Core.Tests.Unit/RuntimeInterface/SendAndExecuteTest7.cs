// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest7 : BaseTest
    {
        public SendAndExecuteTest7(ITestOutputHelper output)
            : base(output)
        { }

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
                var runtime = this.Id.Runtime;
                var m = await runtime.CreateMachineAndExecuteAsync(typeof(M));
                var handled = await runtime.SendEventAndExecuteAsync(m, new E());
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
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
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
            Assert.Equal("Machine 'Microsoft.PSharp.Core.Tests.Unit.SendAndExecuteTest7+M(1)' received event 'Microsoft.PSharp.Core.Tests.Unit.SendAndExecuteTest7+E' that cannot be handled.",
                message);
        }
    }
}
