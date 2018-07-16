//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest5.cs">
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
    public class SendAndExecuteTest5 : BaseTest
    {
        class E : Event
        {
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var runtime = RuntimeService.GetRuntime(this.Id);
                var m = await runtime.CreateMachineAndExecute(typeof(M));
                var handled = await runtime.SendEventAndExecute(m, new E());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
            }
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            class Init : MachineState { }

            void HandleE()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Monitor<SafetyMonitor>(new M_Halts());
            }
        }

        class M_Halts : Event { }
        class SE_Returns : Event { }

        class SafetyMonitor: Monitor
        {
            bool M_halted = false;
            bool SE_returned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(M_Halts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SE_Returns), nameof(OnSEReturns))]
            class Init : MonitorState { }

            [Cold]
            class Done : MonitorState { }

            void OnMHalts()
            {
                this.Assert(SE_returned == false);
                M_halted = true;
            }

            void OnSEReturns()
            {
                this.Assert(M_halted);
                SE_returned = true;
                this.Goto<Done>();
            }
        }

        [Fact]
        public void TestMachineHaltsOnSendExec()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(Harness));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertSucceeded(config, test);
        }

    }
}
