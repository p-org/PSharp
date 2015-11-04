using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class Environment : Machine
    {
        internal class NotifyLeaderUpdate : Event
        {
            public MachineId Leader;

            public NotifyLeaderUpdate(MachineId leader)
                : base()
            {
                this.Leader = leader;
            }
        }

        MachineId[] Servers;
        MachineId Client;

        int NumberOfServers;

		[Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(NotifyNewLeaderUpdate))]
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
                this.Send(this.Servers[idx], new Server.ConfigureEvent(idx, this.Servers, this.Id));
            }

            this.Client = this.CreateMachine(typeof(Client));
        }

        void NotifyNewLeaderUpdate()
        {
            var leader = (this.ReceivedEvent as NotifyLeaderUpdate).Leader;
            this.Send(this.Client, new Client.NotifyServer(leader));
        }
    }
}
