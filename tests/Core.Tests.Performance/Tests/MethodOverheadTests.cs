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
    [Config(typeof(Configuration))]
    public class MethodOverheadTest
    {
        class Node : Machine
        {
            internal class CounterEvent : Event
            {
                public TaskCompletionSource<bool> TCS;
                public int Size;
                public int Counter;

                internal CounterEvent(TaskCompletionSource<bool> tcs, int size)
                {
                    this.TCS = tcs;
                    this.Size = size;
                    this.Counter = 0;
                }

                // To be used with Set()
                internal CounterEvent()
                {
                }

                internal void Set(TaskCompletionSource<bool> tcs, int size)
                {
                    this.TCS = tcs;
                    this.Size = size;
                }

                internal void Increment()
                {
                    var result = Interlocked.Increment(ref this.Counter);
                    if (result == this.Size)
                    {
                        this.TCS.TrySetResult(true);
                    }
                }
            }

            internal class CounterEventWrapper : Event
            {
                internal CounterEvent CounterEvent;

                internal void Set(CounterEvent counterEvent)
                {
                    this.CounterEvent = counterEvent;
                }

                internal void Increment()
                {
                    this.CounterEvent.Increment();
                }
            }

            internal class E1 : CounterEventWrapper { }
            internal class E2 : CounterEventWrapper { }
            internal class E3 : CounterEventWrapper { }
            internal class E4 : CounterEventWrapper { }
            internal class E5 : CounterEventWrapper { }
            internal class E6 : CounterEventWrapper { }
            internal class E7 : CounterEventWrapper { }
            internal class E8 : CounterEventWrapper { }
            internal class E9 : CounterEventWrapper { }
            internal class E10 : CounterEventWrapper { }

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
                (this.ReceivedEvent as CounterEvent).Increment();
            }

            private void OnAct()
            {
                (this.ReceivedEvent as CounterEventWrapper).Increment();
            }

            private void Act1() { this.OnAct(); }
            private void Act2() { this.OnAct(); }
            private void Act3() { this.OnAct(); }
            private void Act4() { this.OnAct(); }
            private void Act5() { this.OnAct(); }
            private void Act6() { this.OnAct(); }
            private void Act7() { this.OnAct(); }
            private void Act8() { this.OnAct(); }
            private void Act9() { this.OnAct(); }
            private void Act10() { this.OnAct(); }
        }

        [Params(100, 500, 1000)]
        public int Size { get; set; }

        [Params(1, 10, 20)]
        public int Reps { get; set; }

        [Benchmark]
        public void CreateAndRunMachines()
        {
            var runtime = new StateMachineRuntime();

            var tcsMachines = new TaskCompletionSource<bool>();
            var configureEvent = new Node.CounterEvent(tcsMachines, Size);

            var machineIds = new List<MachineId>();
            for (int idx = 0; idx < Size; idx++)
            {
                machineIds.Add(runtime.CreateMachine(typeof(Node), null, configureEvent, null));
            }

            tcsMachines.Task.Wait();

            var events = new Node.CounterEventWrapper[] {
                new Node.E1(),
                new Node.E2(),
                new Node.E3(),
                new Node.E4(),
                new Node.E5(),
                new Node.E6(),
                new Node.E7(),
                new Node.E8(),
                new Node.E9(),
                new Node.E10()
            };

            for (var mid = 0; mid < machineIds.Count; ++mid)
            {
                for (var rep = 0; rep < Reps; ++rep)
                {
                    var tcsEvents = new TaskCompletionSource<bool>();
                    var counterEvent = new Node.CounterEvent(tcsEvents, events.Length);
                    for (var evt = 0; evt < events.Length; ++evt)
                    {
                        events[evt].Set(counterEvent);
                        runtime.SendEvent(machineIds[mid], events[evt]);
                    }
                    tcsEvents.Task.Wait();
                }
            }
        }
    }
}
