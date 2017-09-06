using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Workflow.WorkFlowEvents;

namespace Workflow
{
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
            Console.WriteLine("{0} Finished Processing", e.message);
            Console.WriteLine("{0} nodes remaining", numberOfNodes);
            if (numberOfNodes <= 0)
            {
                Console.WriteLine("{0} is the value computed", e.total);
                this.hasCompleted.SetResult(true);
            }
        }
    }
}
