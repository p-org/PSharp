//-----------------------------------------------------------------------
// <copyright file="SharedCounterProductionTest2.cs">
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
    public class SharedCounterProductionTest2 : BaseTest
    {
        const int N = 1000000;

        class E : Event
        {
            public ISharedCounter counter;
            public TaskCompletionSource<bool> tcs;

            public E(ISharedCounter counter, TaskCompletionSource<bool> tcs)
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

                for (int i = 0; i < N; i++)
                {
                    int v;

                    // Add 5
                    do
                    {
                        v = counter.GetValue();

                    } while (v != counter.CompareExchange(v + 5, v));

                    counter.Add(15);
                    counter.Add(-10);
                }

                tcs.SetResult(true);
            }
        }

        [Fact]
        public void TestCounter()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var counter = SharedCounter.Create(runtime, 0);
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
            var m2 = runtime.CreateMachine(typeof(M), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
            Assert.True(counter.GetValue() == N * 20);
        }
    }
}
