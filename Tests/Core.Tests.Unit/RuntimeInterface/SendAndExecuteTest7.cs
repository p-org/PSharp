﻿//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest7.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

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
            var message = "";

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
            Assert.Equal($"Machine '({typeof(M).FullName})-0' received event '{typeof(E).FullName}' that cannot be handled.", message);
        }

    }
}
