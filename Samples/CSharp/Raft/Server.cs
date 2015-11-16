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
            public MachineId Environment;

            public ConfigureEvent(int id, MachineId[] servers, MachineId env)
                : base()
            {
                this.Id = id;
                this.Servers = servers;
                this.Environment = env;
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
        /// The environment machine.
        /// </summary>
        MachineId Environment;

        /// <summary>
        /// The servers.
        /// </summary>
        MachineId[] Servers;

        /// <summary>
        /// Leader id.
        /// </summary>
        MachineId LeaderId;

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

            this.LeaderId = null;
            this.VotedFor = null;
        }

        void Configure()
        {
            this.ServerId = (this.ReceivedEvent as ConfigureEvent).Id;
            this.Servers = (this.ReceivedEvent as ConfigureEvent).Servers;
            this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;

            this.Timer = this.CreateMachine(typeof(Timer));
            this.Send(this.Timer, new Timer.ConfigureEvent(this.Id));
            this.Send(this.Timer, new Timer.StartTimer());

            this.Raise(new BecomeFollower());
        }

        [OnEntry(nameof(FollowerOnInit))]
        [OnEventDoAction(typeof(Client.Request), nameof(RedirectClientRequest))]
        [OnEventDoAction(typeof(Timer.Timeout), nameof(StartLeaderElection))]
        [OnEventDoAction(typeof(VoteRequest), nameof(Vote))]
        [OnEventDoAction(typeof(AppendEntries), nameof(TryAppendEntries))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsFollower))]
        [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsFollower))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
        class Follower : MachineState { }

        void FollowerOnInit()
        {
            this.LeaderId = null;
            this.VotedFor = null;
            this.Send(this.Timer, new Timer.ResetTimer());
        }

        void RedirectClientRequest()
        {
            if (this.LeaderId != null)
            {
                this.Send(this.LeaderId, this.ReceivedEvent);
            }
            else
            {
                var request = this.ReceivedEvent as Client.Request;
                this.Send(request.Client, new Client.ResponseError());
            }
        }

        void StartLeaderElection()
        {
            this.Raise(new BecomeCandidate());
        }

        void Vote()
        {
            var request = this.ReceivedEvent as VoteRequest;

            Console.WriteLine("\nvote terms: " + this.CurrentTerm + " " + request.Term + "\n");

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Send(this.Id, this.ReceivedEvent);
                this.Raise(new BecomeFollower());
            }
            else if (request.Term < this.CurrentTerm)
            {
                Console.WriteLine("\nvote: " + this.ServerId + " false\n");
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, false));
            }
            else if ((this.VotedFor == null || this.VotedFor == request.CandidateId) &&
                request.LastLogIndex >= 0) // temporary
            {
                Console.WriteLine("\nvote: " + this.ServerId + " true\n");
                this.VotedFor = request.CandidateId;
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, true));
            }
        }

        void TryAppendEntries()
        {
            var request = this.ReceivedEvent as AppendEntries;

            Console.WriteLine("\nappend terms: " + this.CurrentTerm + " " + request.Term + "\n");

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Send(this.Id, this.ReceivedEvent);
                this.Raise(new BecomeFollower());
            }
            else if (request.Term < this.CurrentTerm)
            {
                Console.WriteLine("\nappend: " + this.ServerId + " false\n");
                this.Send(this.Timer, new Timer.ResetTimer());
                this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm, false));
            }
            else
            {
                this.LeaderId = request.LeaderId;
                this.Send(this.Timer, new Timer.ResetTimer());

                if (request.PrevLogIndex > 0)
                {
                    // TODO
                }

                this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm, true));
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

        void RespondAppendEntriesAsFollower()
        {
            var request = this.ReceivedEvent as AppendEntriesResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
            }
        }

        [OnEntry(nameof(CandidateOnInit))]
        [OnEventDoAction(typeof(VoteRequest), nameof(Vote))]
        [OnEventDoAction(typeof(AppendEntries), nameof(TryAppendEntries))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsCandidate))]
        [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsCandidate))]
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
                    Console.WriteLine("\nleader: " + this.ServerId + " in term " + this.CurrentTerm +
                        " with " + this.VotesReceived + " votes\n");
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

        void RespondAppendEntriesAsCandidate()
        {
            var request = this.ReceivedEvent as AppendEntriesResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Raise(new BecomeFollower());
            }
        }

        [OnEntry(nameof(LeaderOnInit))]
        [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
        [OnEventDoAction(typeof(VoteRequest), nameof(Vote))]
        [OnEventDoAction(typeof(AppendEntries), nameof(TryAppendEntriesAsLeader))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsLeader))]
        [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsLeader))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        class Leader : MachineState { }

        void LeaderOnInit()
        {
            this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyLeaderElected(this.Id, this.CurrentTerm));

            this.Send(this.Environment, new Environment.NotifyLeaderUpdate(this.Id));

            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;
                this.Send(this.Servers[idx], new AppendEntries(this.CurrentTerm, this.Id,
                    0, 0, new Log[0], this.CommitIndex)); // temporary
            }
        }

        void ProcessClientRequest()
        {
            var request = this.ReceivedEvent as Client.Request;
            Console.WriteLine("\nleader: new client request " + request.Command + "\n");
        }

        void TryAppendEntriesAsLeader()
        {
            var request = this.ReceivedEvent as AppendEntries;

            Console.WriteLine("\nappend terms: " + this.CurrentTerm + " " + request.Term + "\n");

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Send(this.Id, this.ReceivedEvent);
                this.Raise(new BecomeFollower());
            }
        }

        void RespondVoteAsLeader()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Raise(new BecomeFollower());
            }
        }

        void RespondAppendEntriesAsLeader()
        {
            var request = this.ReceivedEvent as AppendEntriesResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.Raise(new BecomeFollower());
            }
        }
    }
}
