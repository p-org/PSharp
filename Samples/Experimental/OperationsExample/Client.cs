using static OperationsExample.Events.ClientEvents;
using Microsoft.PSharp;
using OperationsExample.Events;

namespace OperationsExample
{
    internal class Client : Machine
    {
        MachineId Master;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(local), typeof(PumpRequestOne))]
        class Init : MachineState { }
        
        void Configure()
        {
            this.Master = (this.ReceivedEvent as Config).Master;
            this.Raise(new local());
        }

        [OnEntry(nameof(PumpRequestOneOnEntry))]
        [OnEventGotoState(typeof(MasterEvents.Response), typeof(PumpRequestTwo))]
        class PumpRequestOne : MachineState { }

        void PumpRequestOneOnEntry()
        {
            // Some writes and reads that might/will get wrong
        }

        [OnEntry(nameof(PumpRequestTwoOnEntry))]
        [OnEventGotoState(typeof(local), typeof(Done))]
        class PumpRequestTwo : MachineState { }

        void PumpRequestTwoOnEntry()
        {
            this.Raise(new local());
        }

        [OnEntry(nameof(DoneOnEntry))]
        class Done : MachineState { }

        void DoneOnEntry()
        {
            this.Raise(new Halt());
        }
    }
}
