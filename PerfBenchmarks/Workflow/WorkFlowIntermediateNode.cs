using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using static Workflow.WorkFlowEvents;

namespace Workflow
{
    class WorkFlowIntermediateNode  : Machine
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
}
