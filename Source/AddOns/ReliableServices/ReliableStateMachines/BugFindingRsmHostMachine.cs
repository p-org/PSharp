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
        Type machineType;
        RsmInitEvent initEvent;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(WildCardEvent), nameof(Exec))]
        class Init : MachineState { }

        private async Task InitOnEntry()
        {
            var ev = (this.ReceivedEvent as BugFindingRsmHostMachineInitEvent);
            this.Host = ev.Host;
            this.initEvent = ev.StartingEvent;
            this.machineType = ev.MachineType;
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
        public Type MachineType;
        public RsmInitEvent StartingEvent;

        public BugFindingRsmHostMachineInitEvent(BugFindingRsmHost host, Type machineType, RsmInitEvent startingEvent)
        {
            this.Host = host;
            this.MachineType = machineType;
            this.StartingEvent = startingEvent;
        }
    }
}
