//-----------------------------------------------------------------------
// <copyright file="IgnoreSentEventTest.cs">
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
    public class IgnoreSentEventTest : BaseTest
    {
        public IgnoreSentEventTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }
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
            [OnEventDoAction(typeof(E), nameof(Foo))]
            [IgnoreEvents(typeof(Unit))]
            class Init : MachineState { }

            void Foo()
            {
                this.Send(this.Id, new Unit());
            }
        }

        class B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Foo))]
            [IgnoreEvents(typeof(Unit))]
            class Init : MachineState { }

            void Foo()
            {
                this.Send(this.Id, new Unit<int>());
            }
        }

        class C : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Foo))]
            [IgnoreEvents(typeof(Unit<int>))]
            class Init : MachineState { }

            void Foo()
            {
                this.Send(this.Id, new Unit<int>());
            }
        }

        class D : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Foo))]
            [IgnoreEvents(typeof(Unit<int>))]
            class Init : MachineState { }

            void Foo()
            {
                this.Send(this.Id, new Unit());
            }
        }

        class Harness<T> : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var m = this.CreateMachine(typeof(T));
                this.Send(m, new E());
            }
        }

        [Fact]
        public void TestIgnoreSentEventHandled()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<A>)); });
            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestIgnoreSentEventHandledFailed()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<B>)); });
            string bugReport = $"Machine '{NamespaceName}.IgnoreSentEventTest+B()' received event '{NamespaceName}.IgnoreSentEventTest+Unit`1[[]]' that cannot be handled.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestIgnoreSentGenericEventHandled()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<C>)); });
            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestIgnoreSentGenericEventHandledFailed()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingIterations = 5;
            var test = new Action<IMachineRuntime>((r) => { r.CreateMachine(typeof(Harness<D>)); });
            string bugReport = $"Machine '{NamespaceName}.IgnoreSentEventTest+D()' received event '{NamespaceName}.IgnoreSentEventTest+Unit' that cannot be handled.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
