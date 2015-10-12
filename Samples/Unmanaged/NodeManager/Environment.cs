using System;
using System.Collections.Generic;

using Mocking;

using Microsoft.PSharp;

namespace NodeManager
{
    internal class Environment : Machine
    {
        MachineId NodeManagerMachine;
        List<MachineId> DataNodeMachines;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Events.ConfigAckEvent), typeof(ConfiguringNodes))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.DataNodeMachines = new List<MachineId>();

            this.NodeManagerMachine = this.CreateMachine(typeof(NodeManagerMachine));

            for (int idx = 0; idx < 3; idx++)
            {
                this.DataNodeMachines.Add(this.CreateMachine(typeof(DataNodeMachine)));
            }

            this.Send(this.NodeManagerMachine, new Events.NodeManagerConfigEvent(this.Id, this.DataNodeMachines));
        }
        
        [OnEntry(nameof(ConfiguringNodesOnEntry))]
        class ConfiguringNodes : MachineState { }

        void ConfiguringNodesOnEntry()
        {
            for (int idx = 0; idx < 3; idx++)
            {
                this.Send(this.DataNodeMachines[idx], new Events.DataNodeConfigEvent(this.NodeManagerMachine, idx));
            }
        }
    }
}
