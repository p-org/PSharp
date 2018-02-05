//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest3.cs">
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
    public class SendAndExecuteTest3 
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
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M));
                await this.Runtime.SendEventAndExecute(m, e);
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
            var runtime = PSharpRuntime.Create();
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
