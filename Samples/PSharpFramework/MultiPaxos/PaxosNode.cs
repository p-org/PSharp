using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
main machine GodMachine {
	var paxosnodes : seq[machine];
	var temp : machine;
	var iter : int;
	start state Init {
		entry {
			temp = new PaxosNode((rank = 3,));
			paxosnodes += (0, temp);
			temp = new PaxosNode((rank = 2,));
			paxosnodes += (0, temp);
			temp = new PaxosNode((rank = 1,));
			paxosnodes += (0, temp);
			//send all nodes the other machines
			iter = 0;
			while(iter < sizeof(paxosnodes))
			{
				send paxosnodes[iter], allNodes, (nodes = paxosnodes, );
				iter = iter + 1;
			}
			//create the client nodes
			new Client(paxosnodes);
		}
	}
}


    internal class PaxosNode : Machine
    {
        List<MachineId> PaxosNodes;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Playing))]
        [DeferEvents(Ping)]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Client = this.CreateMachine(typeof(Client), this.Id);
            this.Raise(new Unit());
        }

		[OnEventDoAction(typeof(Unit), nameof(SendPong))]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        class Playing : MachineState
        {
            protected override void OnEntry()
            {
                this.Send((this.Machine as Server).Client, new Pong());
            }
        }

        void SendPong()
        {
            this.Send(this.Client, new Pong());
        }
    }
}
