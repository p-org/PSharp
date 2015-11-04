using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class Environment : Machine
    {
        MachineId[] Servers;
        int NumberOfServers;

		[Start]
        [OnEntry(nameof(EntryOnInit))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.NumberOfServers = 5;
            this.Servers = new MachineId[this.NumberOfServers];

            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.Servers[idx] = this.CreateMachine(typeof(Server));
            }

            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.Send(this.Servers[idx], new Server.ConfigureEvent(idx, this.Servers));
            }
        }
    }
}
