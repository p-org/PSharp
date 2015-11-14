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
            this.CreateMonitor(typeof(M));
            this.NodeManagerMachine = this.CreateMachine(typeof(NodeManagerMachine));
            this.Send(this.NodeManagerMachine, new Events.NodeManagerConfigEvent(this.Id));
        }
        
        [OnEntry(nameof(ConfiguringNodesOnEntry))]
        [OnEventGotoState(typeof(Events.UnitEvent), typeof(InjectFailure))]
        class ConfiguringNodes : MachineState { }

        void ConfiguringNodesOnEntry()
        {
            this.DataNodeMachines = (this.ReceivedEvent as Events.ConfigAckEvent).ids;

            for (int idx = 0; idx < 3; idx++)
            {
                this.Send(this.DataNodeMachines[idx], new Events.DataNodeConfigEvent(this.NodeManagerMachine, idx));
            }

            this.Raise(new Events.UnitEvent());
        }

        [OnEntry(nameof(InjectFailureOnEntry))]
        class InjectFailure : MachineState { }

        void InjectFailureOnEntry()
        {
            bool failed = false;
            while (!failed)
            {
                foreach (var node in this.DataNodeMachines)
                {
                    if (this.Random())
                    {
                        this.Send(node, new Events.FailureEvent());
                        failed = true;
                        break;
                    }
                }
            }
        }
    }
}
