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

                MachineId b = this.CreateMachine(typeof(B), "B");
                MachineId c = this.CreateMachine(typeof(C), new E2(b));

                if (e.ExecuteSynchronously)
                {
                    await this.Runtime.SendEventAndExecute(b, new E1());
                }
                else
                {
                    this.Runtime.SendEvent(b, new E1());
                }
            }
        }

        class B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleEventE1))]
            [OnEventDoAction(typeof(E3), nameof(HandleEventE3))]
            class Init : MachineState { }

            async Task HandleEventE1()
            {
                await this.Receive(typeof(E1));
            }

            void HandleEventE3() { }
        }

        class C : Machine
        {
            MachineId Target;

            [Start]
            [OnEntry(nameof(Run))]
            class Init : MachineState { }

            void Run()
            {
                this.Target = (this.ReceivedEvent as E2).Id;
                this.Send(this.Target, new E3());
                this.Send(this.Target, new E1());
                this.Send(this.Target, new E1());
                this.Send(this.Target, new E1());
            }
        }

        [Fact]
        public void TestSendAndExecute1WithReceive()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A), new Configure(false));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestSendAndExecute1WithReceiveFail()
        {
            var config = Configuration.Create().WithNumberOfIterations(10);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A), new Configure(true));
            });

            base.AssertFailed(config, test, "Machine 'B()' called receive while executing synchronously.");
        }
    }
}
