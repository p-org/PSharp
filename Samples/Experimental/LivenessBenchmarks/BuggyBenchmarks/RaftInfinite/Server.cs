using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace RaftInfinite
{
    /// <summary>
    /// A server in Raft can be one of the following three roles:
    /// follower, candidate or leader.
    /// </summary>
    internal class Server : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the server.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public int Id;
            public MachineId[] Servers;
            public MachineId ClusterManager;

            public ConfigureEvent(int id, MachineId[] servers, MachineId manager)
                : base()
            {
                this.Id = id;
                this.Servers = servers;
                this.ClusterManager = manager;
            }
        }

        /// <summary>
        /// Initiated by candidates during elections.
        /// </summary>
        public class VoteRequest : Event
        {
            public int Term; // candidate’s term
            public MachineId CandidateId; // candidate requesting vote

            public VoteRequest(int term, MachineId candidateId)
                : base()
            {
                this.Term = term;
                this.CandidateId = candidateId;
            }
        }

        /// <summary>
        /// Response to a vote request.
        /// </summary>
        public class VoteResponse : Event
        {
            public int Term; // currentTerm, for candidate to update itself
            public bool VoteGranted; // true means candidate received vote

            public VoteResponse(int term, bool voteGranted)
                : base()
            {
                this.Term = term;
                this.VoteGranted = voteGranted;
            }
        }

        // Events for transitioning a server between roles.
        private class BecomeFollower : Event { }
        private class BecomeCandidate : Event { }
        private class BecomeLeader : Event { }

        internal class ShutDown : Event { }

        #endregion

        #region fields

        /// <summary>
        /// The id of this server.
        /// </summary>
        int ServerId;

        /// <summary>
        /// The cluster manager machine.
        /// </summary>
        MachineId ClusterManager;

        /// <summary>
        /// The servers.
        /// </summary>
        MachineId[] Servers;

        /// <summary>
        /// Leader id.
        /// </summary>
        MachineId LeaderId;

        /// <summary>
        /// The election timer of this server.
        /// </summary>
        MachineId ElectionTimer;

        /// <summary>
        /// Latest term server has seen (initialized to 0 on
        /// first boot, increases monotonically).
        /// </summary>
        int CurrentTerm;

        /// <summary>
        /// Candidate id that received vote in current term (or null if none).
        /// </summary>
        MachineId VotedFor;

        /// <summary>
        /// Index of highest log entry known to be committed (initialized
        /// to 0, increases monotonically). 
        /// </summary>
        int CommitIndex;

        /// <summary>
        /// Index of highest log entry applied to state machine (initialized
        /// to 0, increases monotonically).
        /// </summary>
        int LastApplied;

        /// <summary>
        /// Number of received votes.
        /// </summary>
        int VotesReceived;

        #endregion

        #region initialization

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [DeferEvents(typeof(VoteRequest))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.CurrentTerm = 0;

            this.LeaderId = null;
            this.VotedFor = null;
            
            this.CommitIndex = 0;
            this.LastApplied = 0;
        }

        void Configure()
        {
            this.ServerId = (this.ReceivedEvent as ConfigureEvent).Id;
            this.Servers = (this.ReceivedEvent as ConfigureEvent).Servers;
            this.ClusterManager = (this.ReceivedEvent as ConfigureEvent).ClusterManager;

            this.ElectionTimer = this.CreateMachine(typeof(ElectionTimer));
            this.Send(this.ElectionTimer, new ElectionTimer.ConfigureEvent(this.Id));

            this.Raise(new BecomeFollower());
        }

        #endregion

        #region follower

        [OnEntry(nameof(FollowerOnInit))]
        [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsFollower))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsFollower))]
        [OnEventDoAction(typeof(ElectionTimer.Timeout), nameof(StartLeaderElection))]
        [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
        class Follower : MachineState { }

        void FollowerOnInit()
        {
            this.LeaderId = null;
            this.VotesReceived = 0;

            this.Send(this.ElectionTimer, new ElectionTimer.StartTimer());
        }

        void StartLeaderElection()
        {
            this.Raise(new BecomeCandidate());
        }

        void VoteAsFollower()
        {
            var request = this.ReceivedEvent as VoteRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
            }

            this.Vote(this.ReceivedEvent as VoteRequest);
        }

        void RespondVoteAsFollower()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
            }
        }

        #endregion

        #region candidate

        [OnEntry(nameof(CandidateOnInit))]
        [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsCandidate))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsCandidate))]
        [OnEventDoAction(typeof(ElectionTimer.Timeout), nameof(StartLeaderElection))]
        [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
        [OnEventGotoState(typeof(BecomeLeader), typeof(Leader))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
        class Candidate : MachineState { }

        void CandidateOnInit()
        {
            this.CurrentTerm++;
            this.VotedFor = this.Id;
            this.VotesReceived = 1;

            this.Send(this.ElectionTimer, new ElectionTimer.StartTimer());

            this.BroadcastVoteRequests();
        }

        void BroadcastVoteRequests()
        {
            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;

                this.Send(this.Servers[idx], new VoteRequest(this.CurrentTerm, this.Id));
            }
        }

        void VoteAsCandidate()
        {
            var request = this.ReceivedEvent as VoteRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
                this.Vote(this.ReceivedEvent as VoteRequest);
                this.Raise(new BecomeFollower());
            }
            else
            {
                this.Vote(this.ReceivedEvent as VoteRequest);
            }
        }

        void RespondVoteAsCandidate()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
                this.Raise(new BecomeFollower());
            }
            else if (request.Term != this.CurrentTerm)
            {
                return;
            }

            if (request.VoteGranted)
            {
                this.VotesReceived++;
                if (this.VotesReceived >= (this.Servers.Length / 2) + 1)
                {
                    this.VotesReceived = 0;
                    this.Raise(new BecomeLeader(), true);
                }
            }
        }

        
        #endregion

        #region leader

        [OnEntry(nameof(LeaderOnInit))]
        [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsLeader))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsLeader))]
        [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [IgnoreEvents(typeof(ElectionTimer.Timeout))]
        class Leader : MachineState { }

        void LeaderOnInit()
        {
            Console.WriteLine("[Leader] " +  ServerId + "|" + CurrentTerm);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyLeaderElected());
            this.Send(this.ClusterManager, new ClusterManager.NotifyLeaderUpdate(this.Id, this.CurrentTerm));
        }

        void VoteAsLeader()
        {
            var request = this.ReceivedEvent as VoteRequest;

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;

                this.Vote(this.ReceivedEvent as VoteRequest);

                this.Raise(new BecomeFollower());
            }
            else
            {
                this.Vote(this.ReceivedEvent as VoteRequest);
            }
        }

        void RespondVoteAsLeader()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;

                this.Raise(new BecomeFollower());
            }
        }

        #endregion

        #region general methods

        /// <summary>
        /// Processes the given vote request.
        /// </summary>
        /// <param name="request">VoteRequest</param>
        void Vote(VoteRequest request)
        {

            if (request.Term < this.CurrentTerm ||
                (this.VotedFor != null && this.VotedFor != request.CandidateId))
            {
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, false));
            }
            else
            {
                this.VotedFor = request.CandidateId;
                this.LeaderId = null;

                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, true));
            }
        }

        void ShuttingDown()
        {
            this.Send(this.ElectionTimer, new Halt());

            this.Raise(new Halt());
        }

        #endregion
    }
}
