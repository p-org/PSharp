using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring
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
            if(e != null)
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
            if(count % 1000000 == 0)
            {
                Console.WriteLine(count);
            }
            if(count > 0)
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
}
