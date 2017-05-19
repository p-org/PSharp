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
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SendAndExecuteTest2: BaseTest
    {
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
                MachineId b = this.CreateMachine(typeof(B), "B");
                MachineId c = this.CreateMachine(typeof(C), new E2(b));
                await this.Runtime.SendEventAndExecute(b, new E1());
            }
        }

        class B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleEventE1))]
            [OnEventDoAction(typeof(E3), nameof(HandleEventE3))]
            class Init : MachineState { }

            void HandleEventE1() { }

            async Task HandleEventE3()
            {
                await this.Receive(typeof(E1));
            }
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
        public void TestSendAndExecute2WithReceive()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A));
            });

            base.AssertSucceeded(test);
        }
    }
}
