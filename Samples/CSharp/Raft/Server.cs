using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    /// <summary>
    /// A server in Raft can be one of the following three roles:
    /// follower, candidate or leader.
    /// </summary>
    internal class Server : Machine
    {
        /// <summary>
        /// Used to configure the server.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public int Id;
            public MachineId[] Servers;

            public ConfigureEvent(int id, MachineId[] servers)
                : base()
            {
                this.Id = id;
                this.Servers = servers;
            }
        }

        /// <summary>
        /// Initiated by candidates during elections.
        /// </summary>
        public class VoteRequest : Event
        {
            public int Term; // candidate’s term
            public MachineId CandidateId; // candidate requesting vote
            public int LastLogIndex; // index of candidate’s last log entry
            public int LastLogTerm; // term of candidate’s last log entry

            public VoteRequest(int term, MachineId candidateId, int lastLogIndex, int lastLogTerm)
                : base()
            {
                this.Term = term;
                this.CandidateId = candidateId;
                this.LastLogIndex = lastLogIndex;
                this.LastLogTerm = lastLogTerm;
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
        
        /// <summary>
        /// Initiated by leaders to replicate log entries and
        /// to provide a form of heartbeat.
        /// </summary>
        public class AppendEntries : Event
        {
            public int Term; // leader's term
            public MachineId LeaderId; // so follower can redirect clients
            public int PrevLogIndex; // index of log entry immediately preceding new ones 
            public int PrevLogTerm; // term of PrevLogIndex entry
            public Log[] Entries; // log entries to store (empty for heartbeat; may send more than one for efficiency) 
            public int LeaderCommit; // leader’s CommitIndex

            public AppendEntries(int term, MachineId leaderId, int prevLogIndex,
                int prevLogTerm, Log[] entries, int leaderCommit)
                : base()
            {
                this.Term = term;
                this.LeaderId = leaderId;
                this.PrevLogIndex = prevLogIndex;
                this.PrevLogTerm = prevLogTerm;
                this.Entries = entries;
                this.LeaderCommit = leaderCommit;
            }
        }
        
        /// <summary>
        /// Response to an append entries request.
        /// </summary>
        public class AppendEntriesResponse : Event
        {
            public int Term; // current Term, for leader to update itself 
            public bool Success; // true if follower contained entry matching PrevLogIndex and PrevLogTerm 

            public AppendEntriesResponse(int term, bool success)
                : base()
            {
                this.Term = term;
                this.Success = success;
            }
        }

        // Events for transitioning a server between roles.
        private class BecomeFollower : Event { }
        private class BecomeCandidate : Event { }
        private class BecomeLeader : Event { }

        /// <summary>
        /// The id of this server.
        /// </summary>
        int ServerId;

        /// <summary>
        /// The servers.
        /// </summary>
        MachineId[] Servers;

        /// <summary>
        /// The timer of this server.
        /// </summary>
        MachineId Timer;

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
        /// Log entries.
        /// </summary>
        Log[] Logs;

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
        /// For each server, index of the next log entry to send to that
        /// server (initialized to leader last log index + 1). 
        /// </summary>
        int[] NextIndex;

        /// <summary>
        /// For each server, index of highest log entry known to be replicated
        /// on server (initialized to 0, increases monotonically).
        /// </summary>
        int[] MatchIndex;

        /// <summary>
        /// Number of received votes.
        /// </summary>
        int VotesReceived;

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [DeferEvents(typeof(VoteRequest))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.CurrentTerm = 0;
            this.CommitIndex = 0;
            this.LastApplied = 0;

            this.VotedFor = null;
        }

        void Configure()
        {
            this.ServerId = (this.ReceivedEvent as ConfigureEvent).Id;
            this.Servers = (this.ReceivedEvent as ConfigureEvent).Servers;

            this.Timer = this.CreateMachine(typeof(Timer));
            this.Send(this.Timer, new Timer.ConfigureEvent(this.Id));
            this.Send(this.Timer, new Timer.StartTimer());

            this.Raise(new BecomeFollower());
        }

        [OnEntry(nameof(FollowerOnInit))]
        [OnEventDoAction(typeof(Timer.Timeout), nameof(StartLeaderElection))]
        [OnEventDoAction(typeof(VoteRequest), nameof(Vote))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsFollower))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
        class Follower : MachineState { }

        void FollowerOnInit()
        {
            this.VotedFor = null;
            this.Send(this.Timer, new Timer.ResetTimer());
        }

        void StartLeaderElection()
        {
            this.Raise(new BecomeCandidate());
        }

        void Vote()
        {
            var request = this.ReceivedEvent as VoteRequest;

            Console.WriteLine("vote terms: " + this.CurrentTerm + " " + request.Term);

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Send(this.Id, this.ReceivedEvent);
                this.Raise(new BecomeFollower());
            }
            else if (request.Term < this.CurrentTerm)
            {
                Console.WriteLine("vote: " + this.ServerId + " false");
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, false));
            }
            else if ((this.VotedFor == null || this.VotedFor == request.CandidateId) &&
                request.LastLogIndex >= 0) // temporary
            {
                Console.WriteLine("vote: " + this.ServerId + " true");
                this.VotedFor = request.CandidateId;
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, true));
            }
        }

        void RespondVoteAsFollower()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
            }
        }

        [OnEntry(nameof(CandidateOnInit))]
        [OnEventDoAction(typeof(VoteRequest), nameof(Vote))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsCandidate))]
        [OnEventGotoState(typeof(BecomeLeader), typeof(Leader))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [IgnoreEvents(typeof(Timer.Timeout))] // temporary
        class Candidate : MachineState { }

        void CandidateOnInit()
        {
            this.CurrentTerm++;
            this.VotedFor = this.Id;
            this.VotesReceived = 1;

            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;
                this.Send(this.Servers[idx], new VoteRequest(this.CurrentTerm, this.VotedFor, 0, 0)); // temporary
            }
        }

        void RespondVoteAsCandidate()
        {
            var request = this.ReceivedEvent as VoteResponse;

            if (request.VoteGranted)
            {
                this.VotesReceived++;
                if (this.VotesReceived == (this.Servers.Length / 2) + 1)
                {
                    Console.WriteLine("leader: " + this.ServerId + " with " + this.VotesReceived + " votes");
                    this.VotesReceived = 0;
                    this.Raise(new BecomeLeader());
                }
            }

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Raise(new BecomeFollower());
            }
        }

        [OnEventDoAction(typeof(VoteRequest), nameof(Vote))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsLeader))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        class Leader : MachineState { }

        void RespondVoteAsLeader()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Raise(new BecomeFollower());
            }
        }
    }
}
