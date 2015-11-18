using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class ClusterManager : Machine
    {
        #region events

        internal class NotifyLeaderUpdate : Event
        {
            public MachineId Leader;
            public int Term;

            public NotifyLeaderUpdate(MachineId leader, int term)
                : base()
            {
                this.Leader = leader;
                this.Term = term;
            }
        }

        internal class RedirectRequest : Event
        {
            public Event Request;

            public RedirectRequest(Event request)
                : base()
            {
                this.Request = request;
            }
        }

        private class LocalEvent : Event { }

        #endregion

        #region fields

        MachineId[] Servers;
        int NumberOfServers;

        MachineId Leader;
        int LeaderTerm;

        MachineId Client;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Unavailable))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.NumberOfServers = 5;
            this.LeaderTerm = 0;

            this.CreateMonitor(typeof(SafetyMonitor));
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
            this.Send(this.Client, new Client.ConfigureEvent(this.Id));

            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(BecomeAvailable))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Available))]
        [DeferEvents(typeof(Client.Request))]
        class Unavailable : MachineState { }

        void BecomeAvailable()
        {
            this.UpdateLeader(this.ReceivedEvent as NotifyLeaderUpdate);
            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(Client.Request), nameof(SendClientRequestToLeader))]
        [OnEventDoAction(typeof(RedirectRequest), nameof(RedirectClientRequest))]
        [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(RefreshLeader))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Unavailable))]
        class Available : MachineState { }

        void SendClientRequestToLeader()
        {
            this.Send(this.Leader, this.ReceivedEvent);
        }

        void RedirectClientRequest()
        {
            this.Send(this.Id, (this.ReceivedEvent as RedirectRequest).Request);
        }
        
        void RefreshLeader()
        {
            this.UpdateLeader(this.ReceivedEvent as NotifyLeaderUpdate);
        }

        void BecomeUnavailable()
        {

        }

        #endregion

        #region core methods

        /// <summary>
        /// Updates the leader.
        /// </summary>
        /// <param name="request">NotifyLeaderUpdate</param>
        void UpdateLeader(NotifyLeaderUpdate request)
        {
            if (this.LeaderTerm < request.Term)
            {
                this.Leader = request.Leader;
                this.LeaderTerm = request.Term;
            }
        }

        #endregion
    }
}
