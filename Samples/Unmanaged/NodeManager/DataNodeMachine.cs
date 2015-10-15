using System;
using System.Collections.Generic;

using Mocking;

using Microsoft.PSharp;

namespace NodeManager
{
    internal class DataNodeMachine : Machine
    {
        DataNodeWrapper DataNode;
        MachineId NodeManagerMachine;
        int Idx;

        [Start]
        [OnEventDoAction(typeof(Events.DataNodeConfigEvent), nameof(Configure))]
        [OnEventGotoState(typeof(Events.UnitEvent), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.NodeManagerMachine = (this.ReceivedEvent as Events.DataNodeConfigEvent).id;
            this.Idx = (this.ReceivedEvent as Events.DataNodeConfigEvent).idx;

            this.DataNode = new DataNodeWrapper(this.Idx);

            this.Monitor<M>(new Events.NodeCreatedEvent());
            this.Raise(new Events.UnitEvent());
        }
        
        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Events.FailureEvent), nameof(Fail))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.NodeManagerMachine, this.DataNode.get_update());
        }

        void Fail()
        {
            this.Monitor<M>(new Events.FailedEvent(this.Idx));
            this.Send(this.NodeManagerMachine, new Events.FailedEvent(this.Idx));
            this.Raise(new Halt());
        }
    }
}
