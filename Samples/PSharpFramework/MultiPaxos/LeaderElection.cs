using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class LeaderElection : Machine
    {
        List<MachineId> Servers;
        MachineId ParentServer;
        int MyRank;
        Tuple<int, MachineId> CurrentLeader;
        MachineId CommunicateLeaderTimeout;
        MachineId BroadCastTimeout;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(ProcessPings))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Servers = (this.Payload as object[])[0] as List<MachineId>;
            this.ParentServer = (this.Payload as object[])[1] as MachineId;
            this.MyRank = (int)(this.Payload as object[])[2];
            this.CurrentLeader = (this.Payload as object[])[3] as Tuple<int, MachineId>;

            this.CommunicateLeaderTimeout = this.CreateMachine(typeof(Timer), this.Id, 100);
            this.BroadCastTimeout = this.CreateMachine(typeof(Timer), this.Id, 10);

            this.Raise(new local());
        }

        [OnEntry(nameof(ProcessPingsOnEntry))]
        [OnEventGotoState(typeof(timeout), typeof(ProcessPings), nameof(ProcessPingsAction))]
        [OnEventDoAction(typeof(Ping), nameof(CalculateLeader))]
        class ProcessPings : MachineState { }

        void ProcessPingsOnEntry()
        {
            foreach (var server in this.Servers)
            {
                this.Send(server, new Ping(), this.MyRank, this.Id);
            }

            this.Send(this.BroadCastTimeout, new startTimer());
        }

        void ProcessPingsAction()
        {
            var id = this.Payload as MachineId;

            if (this.CommunicateLeaderTimeout.Equals(id))
            {
                this.Assert(this.CurrentLeader.Item1 <= this.MyRank);
                this.Send(this.ParentServer, new newLeader(), this.CurrentLeader);
                this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
                this.Send(this.CommunicateLeaderTimeout, new startTimer());
                this.Send(this.BroadCastTimeout, new cancelTimer());
            }
        }

        void CalculateLeader()
        {
            var rank = (int)(this.Payload as object[])[0];
            var leader = (this.Payload as object[])[1] as MachineId;

            if (rank < this.MyRank)
            {
                this.CurrentLeader = Tuple.Create(rank, leader);
            }
        }
    }
}
