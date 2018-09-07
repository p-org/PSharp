// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest3 : BaseTest
    {
        public SendAndExecuteTest3(ITestOutputHelper output)
            : base(output)
        { }

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
            public int x;

            public E()
            {
                this.x = 0;
            }
        }

        class LE : Event { }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Conf).tcs;
                var e = new E();
                var runtime = this.Id.Runtime;
                var m = await runtime.CreateMachineAndExecuteAsync(typeof(M));
                await runtime.SendEventAndExecuteAsync(m, e);
                this.Assert(e.x == 1);
                tcs.SetResult(true);
            }
        }

        class M : Machine
        {
            bool LE_Handled = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleEventE))]
            [OnEventDoAction(typeof(LE), nameof(HandleEventLE))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new LE());
            }

            void HandleEventLE()
            {
                LE_Handled = true;
            }

            void HandleEventE()
            {
                this.Assert(LE_Handled);
                var e = (this.ReceivedEvent as E);
                e.x = 1;
            }
        }


        [Fact]
        public void TestSyncSendBlocks()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            runtime.CreateMachine(typeof(Harness), new Conf(tcs));
            tcs.Task.Wait(1000);

            Assert.False(failed);
        }
    }
}
