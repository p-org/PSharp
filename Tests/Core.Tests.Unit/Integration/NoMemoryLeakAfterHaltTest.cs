//-----------------------------------------------------------------------
// <copyright file="NoMemoryLeakAfterHaltTest.cs">
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

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class NoMemoryLeakAfterHaltTest
    {
        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        internal class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
                : base()
            {
                this.Id = id;
            }
        }

        internal class Unit : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;

                int counter = 0;
                while (counter < 100)
                {
                    var n = CreateMachine(typeof(N));
                    this.Send(n, new E(this.Id));
                    this.Receive(typeof(E));
                    counter++;
                }

                tcs.SetResult(true);
            }
        }

        class N : Machine
        {
            int[] LargeArray;

            [Start]
            [OnEntry(nameof(Configure))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            class Init : MachineState { }

            void Configure()
            {
                this.LargeArray = new int[10000000];
                this.LargeArray[this.LargeArray.Length - 1] = 1;
            }

            void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
                Raise(new Halt());
            }
        }

        [Fact]
        public void TestNoMemoryLeakAfterHalt()
        {
            var tcs = new TaskCompletionSource<bool>();
            var runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            tcs.Task.Wait();
            runtime.Stop();
        }
    }
}
