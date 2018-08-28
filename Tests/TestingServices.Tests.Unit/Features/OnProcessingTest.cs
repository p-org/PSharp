//-----------------------------------------------------------------------
// <copyright file="OnProcessingTest.cs">
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
    public class OnProcessingTest : BaseTest
    {
        class E : Event { }

        class E1 : Event { }
        class E2 : Event { }
        class E3: Event { }

        class Begin : Event
        {
            public Event Ev;
            public Begin(Event ev)
            {
                this.Ev = ev;
            }
        }

        class End : Event
        {
            public Event Ev;
            public End(Event ev)
            {
                this.Ev = ev;
            }
        }

        // Ensures that machine M1 sees the following calls:
        // OnProcessingBegin(E1), OnProcessingEnd(E1), OnProcessingBegin(E2), OnProcessingEnd(E2)
        class Spec1 : Monitor
        {
            int counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            [OnEventDoAction(typeof(End), nameof(Process))]
            class S1 : MonitorState { }

            [Cold]
            class S2 : MonitorState { }

            void Process()
            {
                if(counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    counter++;
                }
                else if (counter == 1 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E1)
                {
                    counter ++;
                }
                else if (counter == 2 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E2)
                {
                    counter++;
                }
                else if (counter == 3 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E2)
                {
                    counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if(counter == 4)
                {
                    this.Goto<S2>();
                }
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            [OnEventDoAction(typeof(E2), nameof(Process))]
            [OnEventDoAction(typeof(E3), nameof(ProcessE3))]
            class Init : MachineState { }

            void Process()
            {
                this.Raise(new E3());
            }

            void ProcessE3() { }

            protected override Task OnProcessingBegin(Event ev)
            {
                this.Monitor<Spec1>(new Begin(ev));
                return Task.FromResult(true);
            }

            protected override Task OnProcessingEnd(Event ev)
            {
                this.Monitor<Spec1>(new End(ev));
                return Task.FromResult(true);
            }
        }

        [Fact]
        public void TestOnProcessingCalled()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec1));
                var m = r.CreateMachine(typeof(M1), new E());
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2());
            });

            AssertSucceeded(test);
        }

    }
}
