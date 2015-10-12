using System;
using System.Collections.Generic;

using Mocking;

using Microsoft.PSharp;

namespace NodeManager
{
    internal class NodeManagerMachine : Machine
    {
        NodeManagerWrapper NodeManager;

        MachineId Environment;
        List<MachineId> DataNodeMachines;

        [Start]
        [OnEventDoAction(typeof(Events.NodeManagerConfigEvent), nameof(Configure))]
        [OnEventGotoState(typeof(Events.UnitEvent), typeof(Active))]
        class Init : MachineState { }

		void Configure()
        {
            this.Environment = (this.ReceivedEvent as Events.NodeManagerConfigEvent).env_id;
            this.DataNodeMachines = (this.ReceivedEvent as Events.NodeManagerConfigEvent).ids;
            this.NodeManager = new NodeManagerWrapper(this.DataNodeMachines);

            this.Send(this.Environment, new Events.ConfigAckEvent());
            this.Raise(new Events.UnitEvent());
        }
        
        [OnEventDoAction(typeof(Events.MessageEvent), nameof(ProcessMessage))]
        class Active : MachineState { }

        void ProcessMessage()
        {
            this.NodeManager.invoke(this.ReceivedEvent);
        }
    }
}
