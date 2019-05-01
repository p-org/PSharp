// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp;

namespace Broadcast
{


    public class ConfigureEvent : Event
    {
        public List<MachineId> machineIds;
        public ConfigureEvent(List<MachineId> mIds)
        {
            this.machineIds = mIds;
        }
    }

    public class BroadcastMessage : Event { }


    /// <summary>
    /// A simple machine.
    /// </summary>
    class BroadcastMachine : Machine
    {

        [Start]
		[OnEntry(nameof(initialize))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(DoBroadcast))]
		[OnEventDoAction(typeof(BroadcastMessage), nameof(DoInc))]
        class Init : MachineState { }
		
		void initialize(){
			x = 0;
		}

        void DoBroadcast()
        {
			List<MachineId> machineIds = (this.ReceivedEvent as ConfigureEvent).machineIds;
            foreach(MachineId m in machineIds){
				this.Send(m, new BroadcastMessage());
			}
        }
		int x;
		void DoInc(){
			x++;
		}
    }
	
	
    /// <summary>
    /// A simple machine.
    /// </summary>
    class CreatorMachine : Machine
    {
        [Start]
		[OnEntry(nameof(CreateAllMachines))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(CreateAllMachines))]
        class Init : MachineState { }

        void CreateAllMachines()
        {
			List<MachineId> machineIds = new List<MachineId>();
            for(int i=0;i<3;i++){
				machineIds.Add( this.CreateMachine(typeof(BroadcastMachine)) );
			}
			
			foreach(MachineId m in machineIds){
				this.Send(m, new ConfigureEvent(machineIds));
			}
        }
    }
	
}
