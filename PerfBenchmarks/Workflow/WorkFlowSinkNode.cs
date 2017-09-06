using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using static Workflow.WorkFlowEvents;

namespace Workflow
{
    class WorkFlowSinkNode  : Machine
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
}
