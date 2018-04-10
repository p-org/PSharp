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

        private async Task InitOnEntry()
        {
            var ev = (this.ReceivedEvent as BugFindingRsmHostMachineInitEvent);
            this.Host = ev.Host;
            await Host.EventHandlerLoop(true, null);
        }

        private async Task Exec()
        {
            await Host.EventHandlerLoop(false, this.ReceivedEvent);
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
