//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryProductionTest5.cs">
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

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryProductionTest5 : BaseTest
    {
        class E : Event
        {
            public ISharedDictionary<int, string> counter;
            public TaskCompletionSource<bool> tcs;

            public E(ISharedDictionary<int, string> counter, TaskCompletionSource<bool> tcs)
            {
                this.counter = counter;
                this.tcs = tcs;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = (this.ReceivedEvent as E).counter;
                var n = this.CreateMachine(typeof(N), this.ReceivedEvent);

                for (int i = 0; i <= 100000; i++)
                {
                    counter[i] = i.ToString();
                }
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
                var tcs = (this.ReceivedEvent as E).tcs;
                string v;

                while (!counter.TryGetValue(100000, out v)) { }

                for (int i = 100000; i >= 0; i--)
                {
                    var b = counter.TryGetValue(i, out v);
                    this.Assert(b && v == i.ToString());
                }

                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestDictionarySuccess()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += delegate
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }
    }
}
