//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest4.cs">
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
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SendAndExecuteTest4 : BaseTest
    {
        public SendAndExecuteTest4(ITestOutputHelper output)
            : base(output)
        { }

        class LE : Event { }

        class E : Event
        {
            public MachineId mid;

            public E(MachineId mid)
            {
                this.mid = mid;
            }
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var runtime = RuntimeService.GetRuntime(this.Id);
                var m = await runtime.CreateMachineAndExecute(typeof(M), new E(this.Id));
                var handled = await runtime.SendEventAndExecute(m, new LE());
                this.Assert(handled);
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(LE))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E).mid;
                var runtime = RuntimeService.GetRuntime(this.Id);
                var handled = await runtime.SendEventAndExecute(creator, new LE());
                this.Assert(!handled);
            }

        }


        [Fact]
        public void TestSendCycleDoesNotDeadlock()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertSucceeded(config, test);
        }

    }
}
