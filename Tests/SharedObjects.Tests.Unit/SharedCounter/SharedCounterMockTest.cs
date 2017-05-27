//-----------------------------------------------------------------------
// <copyright file="SharedCounterMockTest.cs">
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

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedCounterMockTest : BaseTest
    {
        class E : Event
        {
            public ISharedCounter counter;

            public E(ISharedCounter counter)
            {
                this.counter = counter;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                this.CreateMachine(typeof(N), new E(counter));

                counter.Increment();
                var v = counter.GetValue();
                this.Assert(v == 1);
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = (this.ReceivedEvent as E).counter;
                counter.Decrement();
            }
        }

        [Fact]
        public void TestCounter()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }
    }
}
