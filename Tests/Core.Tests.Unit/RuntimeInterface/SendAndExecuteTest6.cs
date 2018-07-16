//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest6.cs">
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
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest6
    {
        class E : Event { }

        class Config : Event
        {
            public bool HandleException;
            public TaskCompletionSource<bool> tcs;

            public Config(bool handleEx, TaskCompletionSource<bool> tcs)
            {
                this.HandleException = handleEx;
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
                var runtime = RuntimeService.GetRuntime(this.Id);
                var m = await runtime.CreateMachineAndExecute(typeof(M), this.ReceivedEvent);
                var handled = await runtime.SendEventAndExecute(m, new E());
                this.Assert(handled);
                tcs.TrySetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(false);
                return OnExceptionOutcome.ThrowException;
            }
        }

        class M : Machine
        {
            bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.HandleException = (this.ReceivedEvent as Config).HandleException;
            }

            void HandleE()
            {
                throw new Exception();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return HandleException ? OnExceptionOutcome.HandledException : OnExceptionOutcome.ThrowException;
            }
        }

        class SE_Returns : Event { }

        [Fact]
        public void TestHandledExceptionOnSendExec()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };
            runtime.CreateMachine(typeof(Harness), new Config(true, tcs));
            tcs.Task.Wait();

            Assert.False(failed);
        }

        [Fact]
        public void TestUnHandledExceptionOnSendExec()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            var message = String.Empty;

            runtime.OnFailure += delegate (Exception ex)
            {
                if (!failed)
                {
                    message = (ex is MachineActionExceptionFilterException) ? ex.InnerException.Message : ex.Message;
                    failed = true;
                    tcs.TrySetResult(false);
                }
            };
            runtime.CreateMachine(typeof(Harness), new Config(false, tcs));
            tcs.Task.Wait();

            Assert.True(failed);
            Assert.StartsWith("Exception of type 'System.Exception' was thrown", message);
        }
    }
}
