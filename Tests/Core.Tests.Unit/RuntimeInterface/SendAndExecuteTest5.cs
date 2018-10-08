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

using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class SendAndExecuteTest5 : BaseTest
    {
        public SendAndExecuteTest5(ITestOutputHelper output)
            : base(output)
        { }

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
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Conf).tcs;
                var runtime = this.Id.RuntimeProxy;
                var m = await runtime.CreateMachineAndExecuteAsync(typeof(M));
                var handled = await runtime.SendEventAndExecuteAsync(m, new E());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
                tcs.TrySetResult(true);
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

            protected override Task OnHaltAsync()
            {
                this.Monitor<SafetyMonitor>(new M_Halts());
#if NET45
                return Task.FromResult(0);
#else
                return Task.CompletedTask;
#endif
            }
        }

        class M_Halts : Event { }
        class SE_Returns : Event { }

        class SafetyMonitor : Monitor
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
            var configuration = Configuration.Create();
            configuration.EnableMonitorsInProduction = true;

            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(false);
            };
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(Harness), new Conf(tcs));
            tcs.Task.Wait();

            Assert.False(failed);
        }
    }
}
