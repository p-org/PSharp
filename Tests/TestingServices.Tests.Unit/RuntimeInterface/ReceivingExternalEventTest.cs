//-----------------------------------------------------------------------
// <copyright file="ReceivingExternalEventTest.cs">
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

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class ReceivingExternalEventTest : BaseTest
    {
        public ReceivingExternalEventTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public int Value;

            public E(int value)
            {
                this.Value = value;
            }
        }

        class Engine
        {
            public static void Send(IStateMachineRuntime runtime, MachineId target)
            {
                runtime.SendEvent(target, new E(2));
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandlingEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var runtime = RuntimeService.GetRuntime(this.Id);
                Engine.Send(runtime, this.Id);
            }

            void HandlingEvent()
            {
                this.Assert((this.ReceivedEvent as E).Value == 2);
            }
        }
        
        [Fact]
        public void TestReceivingExternalEvents()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertSucceeded(test);
        }
    }
}
