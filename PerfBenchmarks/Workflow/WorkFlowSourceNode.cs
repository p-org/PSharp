using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using static Workflow.WorkFlowEvents;

namespace Workflow
{
    class WorkFlowSourceNode  : Machine
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
}
