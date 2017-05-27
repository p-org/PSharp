//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryMockTest1.cs">
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
    public class SharedDictionaryMockTest1 : BaseTest
    {
        class E : Event
        {
            public ISharedDictionary<int, string> counter;

            public E(ISharedDictionary<int, string> counter)
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
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);
                this.CreateMachine(typeof(N), new E(counter));

                counter.TryAdd(1, "M");

                var v = counter[1];

                this.Assert(v == "M");
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
                counter.TryUpdate(1, "N", "M");
            }
        }

        [Fact]
        public void TestDictionary()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }
    }
}
