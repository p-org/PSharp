//-----------------------------------------------------------------------
// <copyright file="AsyncAwaitTest.cs">
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
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class AsyncAwaitTest : BaseTest
    {
        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            async Task EntryInit()
            {
                this.Send(this.Id, new E());
                await Task.Delay(2);
                this.Send(this.Id, new E());
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            async Task EntryInit()
            {
                this.Send(this.Id, new E());
                await Task.Delay(2).ConfigureAwait(false);
                this.Send(this.Id, new E());
            }
        }

        [Fact]
        public void TestAsyncDelay()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestAsyncDelayWithOtherSynchronizationContext()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(N));
            });

            var bugReport = "Detected synchronization context that is not controlled by the P# runtime.";
            base.AssertFailed(test, bugReport);
        }
    }
}
