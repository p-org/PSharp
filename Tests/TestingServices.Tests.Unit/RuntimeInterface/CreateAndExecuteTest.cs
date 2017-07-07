//-----------------------------------------------------------------------
// <copyright file="CreateAndExecuteTest.cs">
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
    public class CreateAndExecuteTest : BaseTest
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

            async Task InitOnEntry()
            {
                var e = (this.ReceivedEvent as Configure);

                MachineId mid = null;
                if (e.ExecuteSynchronously)
                {
                    mid = await this.Runtime.CreateMachineAndExecute(typeof(N), "N");
                }
                else
                {
                    mid = this.Runtime.CreateMachine(typeof(N), "N");
                }

                this.Send(mid, new E());
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                await this.Receive(typeof(E));
            }
        }

        [Fact]
        public void TestCreateAndExecuteWithReceive()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Configure(false));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestCreateAndExecuteWithReceiveFail()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Configure(true));
            });

            base.AssertFailed(test, "Machine 'N()' called receive while executing synchronously.", true);
        }

    }
}
