//-----------------------------------------------------------------------
// <copyright file="CreateMachinesTest.cs">
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
using System;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using System.Collections.Generic;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// Creates a set of rings of Machines
    /// Each ring then passes around N messages.
    /// This benchmark is adapted from https://github.com/ponylang/ponyc/tree/master/examples/ring
    /// </summary>    
    public class RingTest
    {
        class RingNode : Machine
        {
            /// <summary>
            /// This machine's neighbour in the ring
            /// </summary>
            MachineId next;
            private TaskCompletionSource<bool> hasInitialized;
            MachineId supervisor;

            internal class Config : Event
            {
                public MachineId Next;
                public TaskCompletionSource<bool> hasInitialized { get; private set; }
                public MachineId Supervisor;

                public Config(MachineId Next, TaskCompletionSource<bool> hasInitialized, MachineId supervisor)
                {
                    this.Next = Next;
                    this.hasInitialized = hasInitialized;
                    this.Supervisor = supervisor;
                }
            }

            internal class CompletionEvent : Event { }

            internal class Pass : Event
            {
                public uint count { get; private set; }

                public Pass(uint count)
                {
                    this.count = count;
                }

                public override string ToString()
                {
                    return count.ToString();
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Config), nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var e = this.ReceivedEvent as Config;
                if (e != null)
                {
                    this.next = e.Next;
                    this.hasInitialized = e.hasInitialized;
                    this.supervisor = e.Supervisor;
                    this.Goto<Active>();
                }
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(Pass), nameof(HandlePass))]
            class Active : MachineState { }

            void HandlePass()
            {
                var e = this.ReceivedEvent as Pass;
                uint count = e.count;
                //if(count % 1000000 == 0)
                //{
                //    Console.WriteLine(count);
                //}
                if (count > 0)
                {
                    this.Send(next, new Pass(e.count - 1));
                }
                else
                {
                    this.Send(supervisor, new CompletionEvent());
                }
            }

            void ActiveOnEntry()
            {
                this.hasInitialized.SetResult(true);
            }

        }

        class SupervisorMachine : Machine
        {
            uint numberOfRings;
            public TaskCompletionSource<bool> hasCompleted { get; private set; }

            internal class Config : Event
            {
                public uint numberOfRings;
                public TaskCompletionSource<bool> hasCompleted { get; private set; }

                public Config(uint numberOfRings, TaskCompletionSource<bool> hasCompleted)
                {
                    this.numberOfRings = numberOfRings;
                    this.hasCompleted = hasCompleted;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(RingNode.CompletionEvent), nameof(HandleCompletion))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var e = this.ReceivedEvent as Config;
                this.numberOfRings = e.numberOfRings;
                this.hasCompleted = e.hasCompleted;
            }

            void HandleCompletion()
            {
                numberOfRings--;
                if (numberOfRings <= 0)
                {
                    this.hasCompleted.SetResult(true);
                }
            }
        }

        [Config(typeof(Configuration))]
        [SimpleJob(RunStrategy.Monitoring)]        
        public class Ringu {
            private List<MachineId> leaders;
            private List<Task> initSignals;
            private PSharpRuntime runtime;
            private TaskCompletionSource<bool> completionSource;

            [Params(5, 10, 15)]
            public uint numberOfRings { get; set; }

            [Params(5, 10, 15)]
            public uint numberOfNodesInRing { get; set; }

            [Params(1000, 10000, 100000, 1000000)]
            public uint numberOfMessagesToPass { get; set; }

            [GlobalSetup]
            public void GlobalSetup()
            {
                runtime = new StateMachineRuntime();
                completionSource = new TaskCompletionSource<bool>();
                leaders = new List<MachineId>();
                initSignals = new List<Task>();

            MachineId supervisor = runtime.CreateMachine(typeof(SupervisorMachine),
                    new SupervisorMachine.Config(numberOfRings, completionSource));

                for (int i = 0; i < numberOfRings; i++)
                {
                    // leader for the current ring
                    MachineId ringLeader = runtime.CreateMachine(typeof(RingNode));
                    MachineId prev = ringLeader, current = ringLeader;
                    for (int j = 1; j < numberOfNodesInRing; j++)
                    {
                        TaskCompletionSource<bool> iSource = new TaskCompletionSource<bool>();
                        initSignals.Add(iSource.Task);
                        current = runtime.CreateMachine(typeof(RingNode));
                        runtime.SendEvent(prev, new RingNode.Config(current, iSource, supervisor));
                        prev = current;
                    }

                    var initSource = new TaskCompletionSource<bool>();
                    initSignals.Add(initSource.Task);
                    runtime.SendEvent(current, new RingNode.Config(ringLeader, initSource, supervisor));
                    leaders.Add(ringLeader);
                }

                Task.WhenAll(initSignals).Wait();
            }

            [Benchmark]
            public void CreateMachines()
            {
                leaders.ForEach(x => runtime.SendEvent(x, new RingNode.Pass(numberOfMessagesToPass)));
                completionSource.Task.Wait();                
            }
        }
        //[Benchmark(Baseline = true)]
        //public void CreateTasks()
        //{
        //    var tcs = new TaskCompletionSource<bool>();
        //    int counter = 0;

        //    for (int idx = 0; idx < Size; idx++)
        //    {
        //        var task = new Task(() => {
        //            int value = Interlocked.Increment(ref counter);
        //            if (value == Size)
        //            {
        //                tcs.TrySetResult(true);
        //            }
        //        });

        //        task.Start();
        //    }

        //    tcs.Task.Wait();
        //}
    }
}
