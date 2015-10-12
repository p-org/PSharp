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

            this.Raise(new Events.UnitEvent());
        }
        
        //[OnEventDoAction(typeof(Events.MessageEvent), nameof(SendPing))]
        [OnEntry(nameof(ActiveOnEntry))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.NodeManagerMachine, this.DataNode.get_update());
        }
    }
}
