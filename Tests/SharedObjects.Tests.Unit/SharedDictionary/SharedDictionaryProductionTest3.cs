//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryProductionTest3.cs">
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

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryProductionTest3 : BaseTest
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
                var tcs = (this.ReceivedEvent as E).tcs;

                for (int i = 0; i < 100; i++)
                {
                    counter.TryAdd(1, "M");
                    counter[1] = "M";
                }

                var c = counter.Count;
                this.Assert(c == 1);
                tcs.SetResult(true);
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

                for (int i = 0; i < 100; i++)
                {
                    counter.TryAdd(1, "N");
                    counter[1] = "N";
                }

                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestDictionaryCount()
        {
            var runtime = PSharpRuntime.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += delegate
            {
                failed = true;
                tcs1.SetResult(true);
                tcs2.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M), new E(counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(N), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
        }
    }
}
