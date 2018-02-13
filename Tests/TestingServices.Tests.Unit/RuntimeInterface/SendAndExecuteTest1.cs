//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest1.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SendAndExecuteTest1 : BaseTest
    {
        class Configure : Event
        {
            public bool ExecuteSynchronously;

            public Configure(bool executeSynchronously)
            {
                this.ExecuteSynchronously = executeSynchronously;
            }
        }

        class E1 : Event { }
        class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
            {
                this.Id = id;
            }
        }
        class E3 : Event { }

        class A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var e = (this.ReceivedEvent as Configure);
                MachineId b;

                if (e.ExecuteSynchronously)
                {
                     b = await this.Runtime.CreateMachineAndExecute(typeof(B)); 
                }
                else
                {
                    b = this.Runtime.CreateMachine(typeof(B));
                }
                this.Send(b, new E1());
            }
        }

        class B : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
            }

        }

        [Fact]
        public void TestSendAndExecuteNoDeadlockWithReceive()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A), new Configure(false));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestSendAndExecuteDeadlockWithReceive()
        {
            var config = Configuration.Create().WithNumberOfIterations(10);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A), new Configure(true));
            });

            base.AssertFailed(config, test, "Livelock detected. 'Microsoft.PSharp.TestingServices.Tests.Unit.SendAndExecuteTest1+A()' and 'Microsoft.PSharp.TestingServices.Tests.Unit.SendAndExecuteTest1+B()' are waiting for an event, but no other schedulable choices are enabled.", true);
        }
    }
}
