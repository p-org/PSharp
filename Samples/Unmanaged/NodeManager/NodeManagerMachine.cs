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
            this.DataNodeMachines = new List<MachineId>();

            for (int idx = 0; idx < 3; idx++)
            {
                this.DataNodeMachines.Add(this.CreateMachine(typeof(DataNodeMachine)));
            }
            
            this.NodeManager = new NodeManagerWrapper(this.DataNodeMachines);

            this.Send(this.Environment, new Events.ConfigAckEvent(this.DataNodeMachines));
            this.Raise(new Events.UnitEvent());
        }
        
        [OnEventDoAction(typeof(Events.MessageEvent), nameof(ProcessMessage))]
        [OnEventDoAction(typeof(Events.FailedEvent), nameof(ProcessFailure))]
        class Active : MachineState { }

        void ProcessMessage()
        {
            this.NodeManager.invoke(this.ReceivedEvent);
        }

        void ProcessFailure()
        {
            var failed = (this.ReceivedEvent as Events.FailedEvent).idx;

            if (this.Random())
            {
                var node = this.CreateMachine(typeof(DataNodeMachine));
                this.DataNodeMachines.Add(node);
                this.Send(node, new Events.DataNodeConfigEvent(this.Id, this.DataNodeMachines.Count - 1));
            }
        }
    }
}
