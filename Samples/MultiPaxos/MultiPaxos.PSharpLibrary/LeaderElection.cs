﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos.PSharpLibrary
{
    internal class LeaderElection : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> Servers;
            public MachineId ParentServer;
            public int MyRank;

            public Config(List<MachineId> servers, MachineId parentServer, int myRank)
                : base(-1, -1)
            {
                this.Servers = servers;
                this.ParentServer = parentServer;
                this.MyRank = myRank;
            }
        }

        internal class Ping : Event
        {
            public MachineId LeaderElection;
            public int Rank;

            public Ping(MachineId leaderElection, int rank)
                : base(-1, -1)
            {
                this.LeaderElection = leaderElection;
                this.Rank = rank;
            }
        }

        internal class NewLeader : Event
        {
            public MachineId CurrentLeader;
            public int Rank;

            public NewLeader(MachineId leader, int rank)
                : base(-1, -1)
            {
                this.CurrentLeader = leader;
                this.Rank = rank;
            }
        }

        List<MachineId> Servers;
        MachineId ParentServer;
        int MyRank;
        Tuple<int, MachineId> CurrentLeader;
        MachineId CommunicateLeaderTimeout;
        MachineId BroadCastTimeout;

        [Start]
        [OnEventGotoState(typeof(local), typeof(ProcessPings))]
        [OnEventDoAction(typeof(LeaderElection.Config), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Servers = (this.ReceivedEvent as LeaderElection.Config).Servers;
            this.ParentServer = (this.ReceivedEvent as LeaderElection.Config).ParentServer;
            this.MyRank = (this.ReceivedEvent as LeaderElection.Config).MyRank;

            this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);

            this.CommunicateLeaderTimeout = this.CreateMachine(typeof(Timer));
            this.Send(this.CommunicateLeaderTimeout, new Timer.Config(this.Id, 100));

            this.BroadCastTimeout = this.CreateMachine(typeof(Timer));
            this.Send(this.BroadCastTimeout, new Timer.Config(this.Id, 10));

            this.Raise(new local());
        }

        [OnEntry(nameof(ProcessPingsOnEntry))]
        [OnEventGotoState(typeof(Timer.Timeout), typeof(ProcessPings), nameof(ProcessPingsAction))]
        [OnEventDoAction(typeof(LeaderElection.Ping), nameof(CalculateLeader))]
        class ProcessPings : MachineState { }

        void ProcessPingsOnEntry()
        {
            foreach (var server in this.Servers)
            {
                this.Send(server, new LeaderElection.Ping(this.Id, this.MyRank));
            }

            this.Send(this.BroadCastTimeout, new Timer.StartTimer());
        }

        void ProcessPingsAction()
        {
            var id = (this.ReceivedEvent as Timer.Timeout).Timer;

            if (this.CommunicateLeaderTimeout.Equals(id))
            {
                this.Assert(this.CurrentLeader.Item1 <= this.MyRank, "this.CurrentLeader <= this.MyRank");
                this.Send(this.ParentServer, new LeaderElection.NewLeader(this.CurrentLeader.Item2, this.CurrentLeader.Item1));
                this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
                this.Send(this.CommunicateLeaderTimeout, new Timer.StartTimer());
                this.Send(this.BroadCastTimeout, new Timer.CancelTimer());
            }
        }

        void CalculateLeader()
        {
            var rank = (this.ReceivedEvent as LeaderElection.Ping).Rank;
            var leader = (this.ReceivedEvent as LeaderElection.Ping).LeaderElection;

            if (rank < this.MyRank)
            {
                this.CurrentLeader = Tuple.Create(rank, leader);
            }
        }
    }
}
