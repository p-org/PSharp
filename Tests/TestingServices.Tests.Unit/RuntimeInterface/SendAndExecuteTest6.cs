//-----------------------------------------------------------------------
// <copyright file="SendAndExecuteTest6.cs">
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
    public class SendAndExecuteTest6 : BaseTest
    {
        class E : Event
        {
        }

        class Config : Event
        {
            public bool HandleException;

            public Config(bool handleEx)
            {
                this.HandleException = handleEx;
            }
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecute(typeof(M), this.ReceivedEvent);
                var handled = await this.Runtime.SendEventAndExecute(m, new E());
                this.Monitor<SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(false);
                return OnExceptionOutcome.ThrowException;
            }
        }

        class M : Machine
        {
            bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.HandleException = (this.ReceivedEvent as Config).HandleException;
            }

            void HandleE()
            {
                throw new Exception();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return HandleException ? OnExceptionOutcome.HandledException : OnExceptionOutcome.ThrowException;
            }
        }

        class SE_Returns : Event { }

        class SafetyMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(SE_Returns), typeof(Done))]
            class Init : MonitorState { }

            [Cold]
            class Done : MonitorState { }

        }

        [Fact]
        public void TestHandledExceptionOnSendExec()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(Harness), new Config(true));
            });
            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestUnHandledExceptionOnSendExec()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(Harness), new Config(false));
            });

            var config = Configuration.Create();
            base.AssertFailed(config, test, 1, bugReports =>
            {
                foreach(var report in bugReports)
                {
                    if(!report.StartsWith("Exception 'System.Exception' was thrown in machine '(Microsoft.PSharp.TestingServices.Tests.Unit.SendAndExecuteTest6+M)-0'"))
                    {
                        return false;
                    }
                }
                return true;
            }, true);
        }

    }
}
