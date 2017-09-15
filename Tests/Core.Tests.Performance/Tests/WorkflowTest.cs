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
using static Microsoft.PSharp.Core.Tests.Performance.WorkflowTest.WorkFlowEvents;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// Simulates a workflow in P#
    /// A bunch of source machines send work to an intermediate machine
    /// Here, the work is an event with payload as a count
    /// The intermediate machine accumulates the received counts and forwards
    /// it to a sink machine
    /// The sink machine simply informs the workflow supervisor when it receives
    /// this total
    /// </summary>
    [SimpleJob(RunStrategy.Monitoring, launchCount: 10, warmupCount: 2, targetCount: 10)]
    public class WorkflowTest
    {
        public class WorkFlowEvents
        {
            internal class WorkFlowCompletionEvent : Event
            {
                public readonly string message;
                public readonly long total;
                public WorkFlowCompletionEvent(string msg, long total = 0)
                {
                    this.message = msg;
                    this.total = total;
                }
            }

            internal class WorkFlowWorkEvent : Event
            {
                public long count { get; private set; }

                public WorkFlowWorkEvent(long count)
                {
                    this.count = count;
                }

                public override string ToString()
                {
                    return count.ToString();
                }
            }

            internal class WorkFlowStartEvent : Event
            {

            }
        }

        class WorkFlowSourceNode : Machine
        {
            /// <summary>
            /// The successors of this machine in the workflow
            /// </summary>
            List<MachineId> next;

            /// <summary>
            /// Tracks whether this machine has initialized
            /// </summary>
            private TaskCompletionSource<bool> hasInitialized;

            MachineId supervisor;

            /// <summary>
            /// The number of work items to generate for the next stage
            /// </summary>
            uint workItemsCount;

            internal class Config : Event
            {
                public List<MachineId> Next;
                public TaskCompletionSource<bool> hasInitialized { get; private set; }
                public uint workItemsCount;
                public MachineId Supervisor;

                public Config(List<MachineId> Next, TaskCompletionSource<bool> hasInitialized, uint workItemsCount, MachineId supervisor)
                {
                    this.Next = Next;
                    this.hasInitialized = hasInitialized;
                    this.Supervisor = supervisor;
                    this.workItemsCount = workItemsCount;
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
                    this.workItemsCount = e.workItemsCount;
                    this.hasInitialized = e.hasInitialized;
                    this.supervisor = e.Supervisor;
                    this.Goto<Active>();
                }
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(WorkFlowStartEvent), nameof(HandleWorkFlowStartEvent))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.hasInitialized.SetResult(true);
            }

            void HandleWorkFlowStartEvent()
            {
                Random rand = new System.Random();
                for (int i = 0; i < workItemsCount; i++)
                {
                    foreach (var succ in next)
                    {
                        this.Send(succ, new WorkFlowEvents.WorkFlowWorkEvent(rand.Next(10, 100)));
                    }
                }
                this.Send(supervisor, new WorkFlowEvents.WorkFlowCompletionEvent(this.ToString()));
            }
        }

        class WorkFlowIntermediateNode : Machine
        {
            /// <summary>
            /// The successors of this machine in the workflow
            /// </summary>
            List<MachineId> next;

            /// <summary>
            /// Tracks whether this machine has initialized
            /// </summary>
            private TaskCompletionSource<bool> hasInitialized;

            MachineId supervisor;

            /// <summary>
            /// The number of message we expect to receive from predecessors of
            /// this node in the workflow
            /// </summary>
            long predecessorMessageCount;

            /// <summary>
            /// The number of messages we actually got
            /// </summary>
            long received = 0;

            long accumulator = 0L;

            internal class Config : Event
            {
                public List<MachineId> Next;
                public TaskCompletionSource<bool> hasInitialized { get; private set; }
                public long predecessorMessageCount;
                public MachineId Supervisor;

                public Config(List<MachineId> Next, TaskCompletionSource<bool> hasInitialized, long predecessorMessagesExpected, MachineId supervisor)
                {
                    this.Next = Next;
                    this.hasInitialized = hasInitialized;
                    this.Supervisor = supervisor;
                    this.predecessorMessageCount = predecessorMessagesExpected;
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
                    this.predecessorMessageCount = e.predecessorMessageCount;
                    this.hasInitialized = e.hasInitialized;
                    this.supervisor = e.Supervisor;
                    this.Goto<Active>();
                }
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(WorkFlowWorkEvent), nameof(HandleWorkFlowWorkEvent))]
            class Active : MachineState { }

            void HandleWorkFlowWorkEvent()
            {
                var e = this.ReceivedEvent as WorkFlowWorkEvent;
                received++;
                accumulator += e.count;
                if (received == predecessorMessageCount)
                {
                    foreach (var succ in next)
                    {
                        this.Send(succ, new WorkFlowWorkEvent(accumulator));
                    }
                    this.Send(supervisor, new WorkFlowCompletionEvent(this.ToString()));
                }
            }

            void ActiveOnEntry()
            {
                this.hasInitialized.SetResult(true);
            }
        }

        class WorkFlowSinkNode : Machine
        {
            /// <summary>
            /// Tracks whether this machine has initialized
            /// </summary>
            private TaskCompletionSource<bool> hasInitialized;

            MachineId supervisor;

            /// <summary>
            /// The number of message we expect to receive from predecessors of
            /// this node in the workflow
            /// </summary>
            long predecessorMessageCount;

            /// <summary>
            /// The number of messages we actually got
            /// </summary>
            long received = 0;

            long accumulator = 0L;

            internal class Config : Event
            {
                public TaskCompletionSource<bool> hasInitialized { get; private set; }
                public long predecessorMessageCount;
                public MachineId Supervisor;

                public Config(TaskCompletionSource<bool> hasInitialized, long predecessorMessagesExpected, MachineId supervisor)
                {
                    this.hasInitialized = hasInitialized;
                    this.Supervisor = supervisor;
                    this.predecessorMessageCount = predecessorMessagesExpected;
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
                    this.hasInitialized = e.hasInitialized;
                    this.supervisor = e.Supervisor;
                    this.predecessorMessageCount = e.predecessorMessageCount;
                    this.Goto<Active>();
                }
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(WorkFlowWorkEvent), nameof(HandleWorkFlowWorkEvent))]
            class Active : MachineState { }

            void HandleWorkFlowWorkEvent()
            {
                var e = this.ReceivedEvent as WorkFlowWorkEvent;
                received++;
                accumulator += e.count;
                if (received == predecessorMessageCount)
                {
                    this.Send(supervisor, new WorkFlowCompletionEvent(this.ToString(), accumulator));
                }
            }

            void ActiveOnEntry()
            {
                this.hasInitialized.SetResult(true);
            }
        }

        class WorkFlowSupervisor : Machine
        {
            uint numberOfNodes;
            public TaskCompletionSource<bool> hasCompleted { get; private set; }

            internal class Config : Event
            {
                public uint numberOfNodes;
                public TaskCompletionSource<bool> hasCompleted { get; private set; }

                public Config(uint numberOfNodes, TaskCompletionSource<bool> hasCompleted)
                {
                    this.numberOfNodes = numberOfNodes;
                    this.hasCompleted = hasCompleted;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(WorkFlowCompletionEvent), nameof(HandleCompletion))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var e = this.ReceivedEvent as Config;
                this.numberOfNodes = e.numberOfNodes;
                this.hasCompleted = e.hasCompleted;
            }

            void HandleCompletion()
            {
                numberOfNodes--;
                var e = this.ReceivedEvent as WorkFlowCompletionEvent;
                //Console.WriteLine("{0} Finished Processing", e.message);
                //Console.WriteLine("{0} nodes remaining", numberOfNodes);
                if (numberOfNodes <= 0)
                {
                    //Console.WriteLine("{0} is the value computed", e.total);
                    this.hasCompleted.SetResult(true);
                }
            }
        }

        [Config(typeof(Configuration))]
        [SimpleJob(RunStrategy.Monitoring)]
        public class Workflow {
            private List<MachineId> sources;
            private List<Task> initSignals;
            private PSharpRuntime runtime;
            private TaskCompletionSource<bool> supervisorCompletionSource;
            private TaskCompletionSource<bool> sinkInitializationSource;
            private TaskCompletionSource<bool> intermediateInitializationSource;

            [Params(2, 4, 8)]
            public uint numberOfSources { get; set; }

            [Params(1000, 10000, 100000, 1000000)]
            public uint numberOfMessages { get; set; }

            [GlobalSetup]
            public void GlobalSetup()
            {
                runtime = new StateMachineRuntime();
                supervisorCompletionSource = new TaskCompletionSource<bool>();
                sinkInitializationSource = new TaskCompletionSource<bool>();
                intermediateInitializationSource = new TaskCompletionSource<bool>();
                sources = new List<MachineId>();
                initSignals = new List<Task>();

                MachineId supervisor = runtime.CreateMachine(typeof(WorkFlowSupervisor),
                    new WorkFlowSupervisor.Config(numberOfSources + 2, supervisorCompletionSource));

                MachineId sink = runtime.CreateMachine(typeof(WorkFlowSinkNode),
                    new WorkFlowSinkNode.Config(sinkInitializationSource, 1, supervisor));

                List<MachineId> next = new List<MachineId>();
                next.Add(sink);
                long numberOfMessagesToIntermediate = (long)numberOfSources * (long)numberOfMessages;

                MachineId intermediate = runtime.CreateMachine(typeof(WorkFlowIntermediateNode),
                    new WorkFlowIntermediateNode.Config(next, intermediateInitializationSource, numberOfMessagesToIntermediate, supervisor));

                var l = new List<MachineId>();
                l.Add(intermediate);
                for (int i = 0; i < numberOfSources; i++)
                {
                    TaskCompletionSource<bool> iSource = new TaskCompletionSource<bool>();
                    initSignals.Add(iSource.Task);
                    var source = runtime.CreateMachine(typeof(WorkFlowSourceNode),
                        new WorkFlowSourceNode.Config(l, iSource, numberOfMessages, supervisor));
                    sources.Add(source);
                }
                
                Task.WhenAll(initSignals).Wait();
            }

            [Benchmark]
            public void CreateMachines()
            {
                sources.ForEach(x => runtime.SendEvent(x, new WorkFlowStartEvent()));
                supervisorCompletionSource.Task.Wait(); 
            }
        }        
    }
}
