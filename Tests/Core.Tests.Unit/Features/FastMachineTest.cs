//-----------------------------------------------------------------------
// <copyright file="DuplicateEventHandlersTest.cs">
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

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class FastMachineTest 
    {
        class E : Event { }

        class Ign : Event { }

        class Def : Event { }

        [Fast]
        class M1: Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check1))]            
            class Init : MachineState { }

            void Check1()
            {
                this.Receive(typeof(E));
            }
            
        }

        [Fast]
        class M2 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(S1))]            
            class Init : MachineState { }

            [DeferEvents(typeof(Def))]
            [OnEventGotoState(typeof(E), typeof(S1))]
            class S1 : MachineState { }
            
        }

        [Fast]
        class M3 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(S1))]
            class Init : MachineState { }

            [IgnoreEvents(typeof(Ign))]
            [OnEventGotoState(typeof(E), typeof(S1))]
            class S1 : MachineState { }

        }
                  
        [Fact]
        public void TestFastMachineReceive()
        {

            var runtime = PSharpRuntime.Create();
            var failed = false;
            string exceptionMessage = "";
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate(Exception ex)
            {
                failed = true;
                tcs.SetResult(true);
                exceptionMessage = ex.Message;                
            };

            var bugReport = "Machine 'Microsoft.PSharp.Core.Tests.Unit.FastMachineTest+M1(0)' " +
                "marked with the Fast attribute performed a Receive action in state " +
                "'Microsoft.PSharp.Core.Tests.Unit.FastMachineTest+M1+Init'.";

            var id = runtime.CreateMachine(typeof(M1));
            runtime.SendEvent(id, new E());
            tcs.Task.Wait(100);
            Assert.True(failed);
            Assert.Equal(bugReport, exceptionMessage);            
        }

        [Fact]
        public void TestFastMachineDeferredEvent()
        {
            var test = new Action(() => {
            var runtime = PSharpRuntime.Create();            
            var id = runtime.CreateMachine(typeof(M2));
            runtime.SendEvent(id, new E());            
            });
            var bugReport = "Machine 'Microsoft.PSharp.Core.Tests.Unit.FastMachineTest+M2(0)' marked with the" +
                " Fast attribute defered/ignored events in state " +
                "'Microsoft.PSharp.Core.Tests.Unit.FastMachineTest+M2+S1'.";

            var ex = Assert.Throws<AssertionFailureException>(test);
            Assert.Equal(bugReport, ex.Message);
        }

        [Fact]
        public void TestFastMachineIgnoredEvent()
        {
            var test = new Action(() => {
                var runtime = PSharpRuntime.Create();
                var id = runtime.CreateMachine(typeof(M3));
                runtime.SendEvent(id, new E());
            });
            var bugReport = "Machine 'Microsoft.PSharp.Core.Tests.Unit.FastMachineTest+M3(0)' marked with the" +
                " Fast attribute defered/ignored events in state " +
                "'Microsoft.PSharp.Core.Tests.Unit.FastMachineTest+M3+S1'.";

            var ex = Assert.Throws<AssertionFailureException>(test);
            Assert.Equal(bugReport, ex.Message);
        }

    }
}
