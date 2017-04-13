//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest.cs">
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

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SendAndExecuteTest : BaseTest
    {
        class Configure : Event
        {
            public bool ExecuteSynchronously;

            public Configure(bool executeSynchronously)
            {
                this.ExecuteSynchronously = executeSynchronously;
            }
        }

        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var e = (this.ReceivedEvent as Configure);

                MachineId mid = this.CreateMachine(typeof(N), "N");

                if (e.ExecuteSynchronously)
                {
                    this.Runtime.SendEventAndExecute(mid, new E());
                }
                else
                {
                    this.Runtime.SendEvent(mid, new E());
                }

                this.Runtime.SendEvent(mid, new E());
            }
        }

        class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(HandleEvent))]
            class Init : MachineState { }

            void HandleEvent()
            {
                this.Receive(typeof(E));
            }
        }

        [Fact]
        public void TestSendAndExecuteWithReceive()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Configure(false));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestSendAndExecuteWithReceiveFail()
        {
            var config = Configuration.Create().WithNumberOfIterations(10);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Configure(true));
            });

            base.AssertFailed(config, test, "Machine 'N()' called receive while executing synchronously.");
        }
    }
}
