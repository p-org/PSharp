//-----------------------------------------------------------------------
// <copyright file="MethodOverheadTest.cs">
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

using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    class E1 : Event { }
    class E2 : Event { }
    class E3 : Event { }
    class E4 : Event { }
    class E5 : Event { }
    class E6 : Event { }
    class E7 : Event { }
    class E8 : Event { }
    class E9 : Event { }
    class E10 : Event { }

    [Config(typeof(Configuration))]
    public class MethodOverheadTest
    {
        class Node : Machine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;
                public int Size;
                public int Counter;

                internal Configure(TaskCompletionSource<bool> tcs, int size)
                {
                    this.TCS = tcs;
                    this.Size = size;
                    this.Counter = 0;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(Act1))]
            [OnEventDoAction(typeof(E2), nameof(Act2))]
            [OnEventDoAction(typeof(E3), nameof(Act3))]
            [OnEventDoAction(typeof(E4), nameof(Act4))]
            [OnEventDoAction(typeof(E5), nameof(Act5))]
            [OnEventDoAction(typeof(E6), nameof(Act6))]
            [OnEventDoAction(typeof(E7), nameof(Act7))]
            [OnEventDoAction(typeof(E8), nameof(Act8))]
            [OnEventDoAction(typeof(E9), nameof(Act9))]
            [OnEventDoAction(typeof(E10), nameof(Act10))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;
                var size = (this.ReceivedEvent as Configure).Size;
                var counter = Interlocked.Increment(ref (this.ReceivedEvent as Configure).Counter);
                if (counter == size)
                {
                    tcs.TrySetResult(true);
                }
            }

            int counter;

            private void Act1() { ++counter; }
            private void Act2() { ++counter; }
            private void Act3() { ++counter; }
            private void Act4() { ++counter; }
            private void Act5() { ++counter; }
            private void Act6() { ++counter; }
            private void Act7() { ++counter; }
            private void Act8() { ++counter; }
            private void Act9() { ++counter; }
            private void Act10() { ++counter; }
        }

        [Params(100, 500, 1000)]
        public int Size { get; set; }

        [Params(1, 10, 100)]
        public int Reps { get; set; }

        [Benchmark]
        public void CreateAndRunMachines()
        {
            var runtime = new StateMachineRuntime();

            var tcs = new TaskCompletionSource<bool>();
            Node.Configure configureEvent = new Node.Configure(tcs, Size);

            var machineIds = new List<MachineId>();
            for (int idx = 0; idx < Size; idx++)
            {
                machineIds.Add(runtime.CreateMachine(typeof(Node), null, configureEvent, null));
            }

            tcs.Task.Wait();

            var events = new Event[] {
                new E1(),
                new E2(),
                new E3(),
                new E4(),
                new E5(),
                new E6(),
                new E7(),
                new E8(),
                new E9(),
                new E10()
            };

            for (var mid = 0; mid < machineIds.Count; ++mid)
            {
                for (var rep = 0; rep < Reps; ++rep)
                {
                    for (var evt = 0; evt < events.Length; ++evt)
                    {
                        runtime.SendEvent(machineIds[mid], events[evt]);
                    }
                }
            }
        }
    }
}
