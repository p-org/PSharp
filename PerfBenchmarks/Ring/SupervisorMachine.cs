using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring
{
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
            if(numberOfRings <= 0)
            {
                this.hasCompleted.SetResult(true);
            }
        }
    }
}
