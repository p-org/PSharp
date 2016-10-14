using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace RaftInfinite
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

        internal class ShutDown : Event { }
        private class LocalEvent : Event { }

        #endregion

        #region fields

        MachineId[] Servers;
        int NumberOfServers;

        MachineId Leader;
        int LeaderTerm;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.NumberOfServers = 5;
            this.LeaderTerm = 0;

            this.Servers = new MachineId[this.NumberOfServers];

            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.Servers[idx] = this.CreateMachine(typeof(Server));
            }

            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(ConfiguringOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Availability.Unavailable))]
        class Configuring : MachineState { }

        void ConfiguringOnInit()
        {
            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.Send(this.Servers[idx], new Server.ConfigureEvent(idx, this.Servers, this.Id));
            }

            this.Raise(new LocalEvent());
        }

        class Availability : StateGroup
        {
            [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(BecomeAvailable))]
            [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Available))]
            public class Unavailable : MachineState { }

            [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(RefreshLeader))]
            [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Unavailable))]
            public class Available : MachineState { }
        }

        void BecomeAvailable()
        {
            this.UpdateLeader(this.ReceivedEvent as NotifyLeaderUpdate);
            this.Raise(new LocalEvent());
        }

        void RefreshLeader()
        {
            this.UpdateLeader(this.ReceivedEvent as NotifyLeaderUpdate);
        }

        void BecomeUnavailable()
        {

        }

        void ShuttingDown()
        {
            for (int idx = 0; idx < this.NumberOfServers; idx++)
            {
                this.Send(this.Servers[idx], new Server.ShutDown());
            }

            this.Raise(new Halt());
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
