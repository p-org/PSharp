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
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SendAndExecuteTest3 : BaseTest
    {
        public SendAndExecuteTest3(ITestOutputHelper output)
            : base(output)
        { }

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
                var e = new E();
                var runtime = this.Id.Runtime;
                var m = await runtime.CreateMachineAndExecuteAsync(typeof(M));
                var handled = await runtime.SendEventAndExecuteAsync(m, e);
                this.Assert(handled);
                this.Assert(e.x == 1);
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
        public void TestSendBlocks()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertSucceeded(config, test);
        }
    }
}
