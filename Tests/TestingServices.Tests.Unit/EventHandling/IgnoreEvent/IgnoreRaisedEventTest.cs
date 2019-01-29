//-----------------------------------------------------------------------
// <copyright file="IgnoreRaisedEventTest.cs">
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
    public class IgnoreRaisedEventTest : BaseTest
    {
        public IgnoreRaisedEventTest(ITestOutputHelper output)
            : base(output)
        { }

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
        class Unit<T> : Event { }

        class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(Unit))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            class Init : MachineState { }

            void Foo()
            {
                this.Raise(new Unit());
            }

            void Bar()
            {
                var e = this.ReceivedEvent as E2;
                this.Send(e.mid, new E2(this.Id));
            }
        }

        class B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(Unit))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            class Init : MachineState { }

            void Foo()
            {
                this.Raise(new Unit<int>());
            }

            void Bar()
            {
                var e = this.ReceivedEvent as E2;
                this.Send(e.mid, new E2(this.Id));
            }
        }

        class C : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(Unit<int>))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            class Init : MachineState { }

            void Foo()
            {
                this.Raise(new Unit<int>());
            }

            void Bar()
            {
                var e = this.ReceivedEvent as E2;
                this.Send(e.mid, new E2(this.Id));
            }
        }

        class D : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(Unit<int>))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            class Init : MachineState { }

            void Foo()
            {
                this.Raise(new Unit());
            }

            void Bar()
            {
                var e = this.ReceivedEvent as E2;
                this.Send(e.mid, new E2(this.Id));
            }
        }

        class Harness<T> : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            async Task InitOnEntry()
            {
                var m = this.CreateMachine(typeof(T));
                this.Send(m, new E1());
                this.Send(m, new E2(this.Id));
                var e = await this.Receive(typeof(E2)) as E2;
            }
        }

        [Fact]
        public void TestIgnoreRaisedEventHandled()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<A>)); });
            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestIgnoreRaisedEventHandledFailed()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<B>)); });
            string bugReport = $"Machine 'B()' received event 'Unit`1[]' that cannot be handled.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestIgnoreRaisedGenericEventHandled()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<C>)); });
            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestIgnoreRaisedGenericEventHandledFailed()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<D>)); });
            string bugReport = $"Machine 'D()' received event 'Unit' that cannot be handled.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
