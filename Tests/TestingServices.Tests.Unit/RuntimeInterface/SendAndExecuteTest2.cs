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

        class A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var b = this.CreateMachine(typeof(B));
                var handled = await this.Runtime.SendEventAndExecute(b, new E1());
                this.Assert(!handled);
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

        class C : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var d = this.CreateMachine(typeof(D));
                var handled = await this.Runtime.SendEventAndExecute(d, new E1());
                this.Assert(handled);
            }
        }

        class D : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void Handle()
            {

            }
        }

        [Fact]
        public void TestSyncSendToReceive()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(A));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestSyncSendSometimesDoesNotHandle()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(C));
            });

            base.AssertFailed(config, test, 1, true);
        }
    }
}
