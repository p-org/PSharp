using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    class BugFindingRsmHostMachine : Machine
    {
        BugFindingRsmHost Host;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(WildCardEvent), nameof(Exec))]
        class Init : MachineState { }

        [OnEntry(nameof(DoneOnEntry))]
        class Done : MachineState { }

        private async Task InitOnEntry()
        {
            var ev = (this.ReceivedEvent as BugFindingRsmHostMachineInitEvent);
            this.Host = ev.Host;
            this.Host.RemoveSpuriousTimeouts = new Func<string, Task>(name =>
            {
                return Receive(typeof(Timers.TimeoutEvent), te => (te as Timers.TimeoutEvent).Name == name);
            });

            await Host.EventHandlerLoop(true, null);
        }

        private async Task Exec()
        {
            await Host.EventHandlerLoop(false, this.ReceivedEvent);

            if(Host.MachineHalted)
            {
                this.Goto<Done>();
            }
        }

        private void DoneOnEntry()
        {
            this.Raise(new Halt());
        }
    }


    class BugFindingRsmHostMachineInitEvent : Event
    {
        public BugFindingRsmHost Host;

        public BugFindingRsmHostMachineInitEvent(BugFindingRsmHost host)
        {
            this.Host = host;
        }
    }
}
