//-----------------------------------------------------------------------
// <copyright file="TestIgnoreRaised.cs">
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
    public class TestIgnoreRaised : BaseTest
    {
        class E1 : Event { }
        class E2 : Event
        {
            public MachineId mid;
            public E2(MachineId mid)
            {
                this.mid = mid;
            }
        }
        class Unit : Event { }


        class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(foo))]
            [IgnoreEvents(typeof(Unit))]
            [OnEventDoAction(typeof(E2), nameof(bar))]
            class Init : MachineState { }

            void foo()
            {
                this.Raise(new Unit());
            }

            void bar()
            {
                var e = this.ReceivedEvent as E2;
                this.Send(e.mid, new E2(this.Id));
            }
        }


        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var m = this.CreateMachine(typeof(A));
                this.Send(m, new E1());
                this.Send(m, new E2(this.Id));
                var e = await this.Receive(typeof(E2)) as E2;
                //Console.WriteLine("Got Response from {0}", e.mid);
            }
        }

        /// <summary>
        /// P# semantics test: testing for ignore of a raised event.
        /// </summary>
        [Fact]
        public void TestIgnoreRaisedEventHandled()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(Harness)); });
            base.AssertSucceeded(configuration, test);
        }
    }
}
