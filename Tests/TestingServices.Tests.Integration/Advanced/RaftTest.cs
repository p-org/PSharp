//-----------------------------------------------------------------------
// <copyright file="RaftTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    /// <summary>
    /// This is a simple implementation of the Raft consensus protocol
    /// described in the following paper:
    /// 
    /// https://raft.github.io/raft.pdf
    /// 
    /// This test contains a bug that leads to duplicate leader election
    /// in the same term.
    /// </summary>
    public class RaftTest : BaseTest
    {
        class Log
        {
            public readonly int Term;
            public readonly int Command;

            public Log(int term, int command)
            {
                this.Term = term;
                this.Command = command;
            }
        }

        class ClusterManager : Machine
        {
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

            internal class ShutDown : Event { }
            private class LocalEvent : Event { }

            MachineId[] Servers;
            int NumberOfServers;

            MachineId Leader;
            int LeaderTerm;

            MachineId Client;

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

                this.Client = this.CreateMachine(typeof(Client));

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

                this.Send(this.Client, new Client.ConfigureEvent(this.Id));

                this.Raise(new LocalEvent());
            }

            class Availability : StateGroup
            {
                [OnEventDoAction(typeof(NotifyLeaderUpdate), nameof(BecomeAvailable))]
                [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
                [OnEventGotoState(typeof(LocalEvent), typeof(Available))]
                [DeferEvents(typeof(Client.Request))]
                public class Unavailable : MachineState { }


                [OnEventDoAction(typeof(Client.Request), nameof(SendClientRequestToLeader))]
                [OnEventDoAction(typeof(RedirectRequest), nameof(RedirectClientRequest))]
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

            void ShuttingDown()
            {
                for (int idx = 0; idx < this.NumberOfServers; idx++)
                {
                    this.Send(this.Servers[idx], new Server.ShutDown());
                }

                this.Raise(new Halt());
            }

            void UpdateLeader(NotifyLeaderUpdate request)
            {
                if (this.LeaderTerm < request.Term)
                {
                    this.Leader = request.Leader;
                    this.LeaderTerm = request.Term;
                }
            }
        }

        /// <summary>
        /// A server in Raft can be one of the following three roles:
        /// follower, candidate or leader.
        /// </summary>
        class Server : Machine
        {
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
            public class AppendEntriesRequest : Event
            {
                public int Term; // leader's term
                public MachineId LeaderId; // so follower can redirect clients
                public int PrevLogIndex; // index of log entry immediately preceding new ones 
                public int PrevLogTerm; // term of PrevLogIndex entry
                public List<Log> Entries; // log entries to store (empty for heartbeat; may send more than one for efficiency) 
                public int LeaderCommit; // leader’s CommitIndex

                public MachineId ReceiverEndpoint; // client

                public AppendEntriesRequest(int term, MachineId leaderId, int prevLogIndex,
                    int prevLogTerm, List<Log> entries, int leaderCommit, MachineId client)
                    : base()
                {
                    this.Term = term;
                    this.LeaderId = leaderId;
                    this.PrevLogIndex = prevLogIndex;
                    this.PrevLogTerm = prevLogTerm;
                    this.Entries = entries;
                    this.LeaderCommit = leaderCommit;
                    this.ReceiverEndpoint = client;
                }
            }

            /// <summary>
            /// Response to an append entries request.
            /// </summary>
            public class AppendEntriesResponse : Event
            {
                public int Term; // current Term, for leader to update itself 
                public bool Success; // true if follower contained entry matching PrevLogIndex and PrevLogTerm 

                public MachineId Server;
                public MachineId ReceiverEndpoint; // client

                public AppendEntriesResponse(int term, bool success, MachineId server, MachineId client)
                    : base()
                {
                    this.Term = term;
                    this.Success = success;
                    this.Server = server;
                    this.ReceiverEndpoint = client;
                }
            }

            // Events for transitioning a server between roles.
            private class BecomeFollower : Event { }
            private class BecomeCandidate : Event { }
            private class BecomeLeader : Event { }

            internal class ShutDown : Event { }

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
            /// The periodic timer of this server.
            /// </summary>
            MachineId PeriodicTimer;

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
            List<Log> Logs;

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
            Dictionary<MachineId, int> NextIndex;

            /// <summary>
            /// For each server, index of highest log entry known to be replicated
            /// on server (initialized to 0, increases monotonically).
            /// </summary>
            Dictionary<MachineId, int> MatchIndex;

            /// <summary>
            /// Number of received votes.
            /// </summary>
            int VotesReceived;

            /// <summary>
            /// The latest client request.
            /// </summary>
            Client.Request LastClientRequest;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
            [DeferEvents(typeof(VoteRequest), typeof(AppendEntriesRequest))]
            class Init : MachineState { }

            void EntryOnInit()
            {
                this.CurrentTerm = 0;

                this.LeaderId = null;
                this.VotedFor = null;

                this.Logs = new List<Log>();

                this.CommitIndex = 0;
                this.LastApplied = 0;

                this.NextIndex = new Dictionary<MachineId, int>();
                this.MatchIndex = new Dictionary<MachineId, int>();
            }

            void Configure()
            {
                this.ServerId = (this.ReceivedEvent as ConfigureEvent).Id;
                this.Servers = (this.ReceivedEvent as ConfigureEvent).Servers;
                this.ClusterManager = (this.ReceivedEvent as ConfigureEvent).ClusterManager;

                this.ElectionTimer = this.CreateMachine(typeof(ElectionTimer));
                this.Send(this.ElectionTimer, new ElectionTimer.ConfigureEvent(this.Id));

                this.PeriodicTimer = this.CreateMachine(typeof(PeriodicTimer));
                this.Send(this.PeriodicTimer, new PeriodicTimer.ConfigureEvent(this.Id));

                this.Raise(new BecomeFollower());
            }

            [OnEntry(nameof(FollowerOnInit))]
            [OnEventDoAction(typeof(Client.Request), nameof(RedirectClientRequest))]
            [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsFollower))]
            [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsFollower))]
            [OnEventDoAction(typeof(AppendEntriesRequest), nameof(AppendEntriesAsFollower))]
            [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsFollower))]
            [OnEventDoAction(typeof(ElectionTimer.Timeout), nameof(StartLeaderElection))]
            [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
            [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
            [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
            [IgnoreEvents(typeof(PeriodicTimer.Timeout))]
            class Follower : MachineState { }

            void FollowerOnInit()
            {
                this.LeaderId = null;
                this.VotesReceived = 0;

                this.Send(this.ElectionTimer, new ElectionTimer.StartTimer());
            }

            void RedirectClientRequest()
            {
                if (this.LeaderId != null)
                {
                    this.Send(this.LeaderId, this.ReceivedEvent);
                }
                else
                {
                    this.Send(this.ClusterManager, new ClusterManager.RedirectRequest(this.ReceivedEvent));
                }
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

            void AppendEntriesAsFollower()
            {
                var request = this.ReceivedEvent as AppendEntriesRequest;
                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;
                }

                this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);
            }

            void RespondAppendEntriesAsFollower()
            {
                var request = this.ReceivedEvent as AppendEntriesResponse;
                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;
                }
            }

            [OnEntry(nameof(CandidateOnInit))]
            [OnEventDoAction(typeof(Client.Request), nameof(RedirectClientRequest))]
            [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsCandidate))]
            [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsCandidate))]
            [OnEventDoAction(typeof(AppendEntriesRequest), nameof(AppendEntriesAsCandidate))]
            [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsCandidate))]
            [OnEventDoAction(typeof(ElectionTimer.Timeout), nameof(StartLeaderElection))]
            [OnEventDoAction(typeof(PeriodicTimer.Timeout), nameof(BroadcastVoteRequests))]
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
                // BUG: duplicate votes from same follower
                this.Send(this.PeriodicTimer, new PeriodicTimer.StartTimer());

                for (int idx = 0; idx < this.Servers.Length; idx++)
                {
                    if (idx == this.ServerId)
                        continue;

                    var lastLogIndex = this.Logs.Count;
                    var lastLogTerm = this.GetLogTermForIndex(lastLogIndex);

                    this.Send(this.Servers[idx], new VoteRequest(this.CurrentTerm, this.Id,
                        lastLogIndex, lastLogTerm));
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
                    return;
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
                        this.Raise(new BecomeLeader());
                    }
                }
            }

            void AppendEntriesAsCandidate()
            {
                var request = this.ReceivedEvent as AppendEntriesRequest;
                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;
                    this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);
                    this.Raise(new BecomeFollower());
                }
                else
                {
                    this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);
                }
            }

            void RespondAppendEntriesAsCandidate()
            {
                var request = this.ReceivedEvent as AppendEntriesResponse;
                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;
                    this.Raise(new BecomeFollower());
                }
            }

            [OnEntry(nameof(LeaderOnInit))]
            [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
            [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsLeader))]
            [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsLeader))]
            [OnEventDoAction(typeof(AppendEntriesRequest), nameof(AppendEntriesAsLeader))]
            [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsLeader))]
            [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
            [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
            [IgnoreEvents(typeof(ElectionTimer.Timeout), typeof(PeriodicTimer.Timeout))]
            class Leader : MachineState { }

            void LeaderOnInit()
            {
                this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyLeaderElected(this.CurrentTerm));
                this.Send(this.ClusterManager, new ClusterManager.NotifyLeaderUpdate(this.Id, this.CurrentTerm));

                var logIndex = this.Logs.Count;
                var logTerm = this.GetLogTermForIndex(logIndex);

                this.NextIndex.Clear();
                this.MatchIndex.Clear();
                for (int idx = 0; idx < this.Servers.Length; idx++)
                {
                    if (idx == this.ServerId)
                        continue;
                    this.NextIndex.Add(this.Servers[idx], logIndex + 1);
                    this.MatchIndex.Add(this.Servers[idx], 0);
                }

                for (int idx = 0; idx < this.Servers.Length; idx++)
                {
                    if (idx == this.ServerId)
                        continue;
                    this.Send(this.Servers[idx], new AppendEntriesRequest(this.CurrentTerm, this.Id,
                        logIndex, logTerm, new List<Log>(), this.CommitIndex, null));
                }
            }

            void ProcessClientRequest()
            {
                this.LastClientRequest = this.ReceivedEvent as Client.Request;

                var log = new Log(this.CurrentTerm, this.LastClientRequest.Command);
                this.Logs.Add(log);

                this.BroadcastLastClientRequest();
            }

            void BroadcastLastClientRequest()
            {
                var lastLogIndex = this.Logs.Count;

                this.VotesReceived = 1;
                for (int idx = 0; idx < this.Servers.Length; idx++)
                {
                    if (idx == this.ServerId)
                        continue;

                    var server = this.Servers[idx];
                    if (lastLogIndex < this.NextIndex[server])
                        continue;

                    var logs = this.Logs.GetRange(this.NextIndex[server] - 1,
                        this.Logs.Count - (this.NextIndex[server] - 1));

                    var prevLogIndex = this.NextIndex[server] - 1;
                    var prevLogTerm = this.GetLogTermForIndex(prevLogIndex);

                    this.Send(server, new AppendEntriesRequest(this.CurrentTerm, this.Id, prevLogIndex,
                        prevLogTerm, logs, this.CommitIndex, this.LastClientRequest.Client));
                }
            }

            void VoteAsLeader()
            {
                var request = this.ReceivedEvent as VoteRequest;

                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;

                    this.RedirectLastClientRequestToClusterManager();
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

                    this.RedirectLastClientRequestToClusterManager();
                    this.Raise(new BecomeFollower());
                }
            }

            void AppendEntriesAsLeader()
            {
                var request = this.ReceivedEvent as AppendEntriesRequest;
                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;

                    this.RedirectLastClientRequestToClusterManager();
                    this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);

                    this.Raise(new BecomeFollower());
                }
            }

            void RespondAppendEntriesAsLeader()
            {
                var request = this.ReceivedEvent as AppendEntriesResponse;
                if (request.Term > this.CurrentTerm)
                {
                    this.CurrentTerm = request.Term;
                    this.VotedFor = null;

                    this.RedirectLastClientRequestToClusterManager();
                    this.Raise(new BecomeFollower());
                    return;
                }
                else if (request.Term != this.CurrentTerm)
                {
                    return;
                }

                if (request.Success)
                {
                    this.NextIndex[request.Server] = this.Logs.Count + 1;
                    this.MatchIndex[request.Server] = this.Logs.Count;

                    this.VotesReceived++;
                    if (request.ReceiverEndpoint != null &&
                        this.VotesReceived >= (this.Servers.Length / 2) + 1)
                    {
                        var commitIndex = this.MatchIndex[request.Server];
                        if (commitIndex > this.CommitIndex &&
                            this.Logs[commitIndex - 1].Term == this.CurrentTerm)
                        {
                            this.CommitIndex = commitIndex;
                        }

                        this.VotesReceived = 0;
                        this.LastClientRequest = null;

                        this.Send(request.ReceiverEndpoint, new Client.Response());
                    }
                }
                else
                {
                    if (this.NextIndex[request.Server] > 1)
                    {
                        this.NextIndex[request.Server] = this.NextIndex[request.Server] - 1;
                    }

                    var logs = this.Logs.GetRange(this.NextIndex[request.Server] - 1,
                        this.Logs.Count - (this.NextIndex[request.Server] - 1));

                    var prevLogIndex = this.NextIndex[request.Server] - 1;
                    var prevLogTerm = this.GetLogTermForIndex(prevLogIndex);

                    this.Send(request.Server, new AppendEntriesRequest(this.CurrentTerm, this.Id, prevLogIndex,
                        prevLogTerm, logs, this.CommitIndex, request.ReceiverEndpoint));
                }
            }

            /// <summary>
            /// Processes the given vote request.
            /// </summary>
            /// <param name="request">VoteRequest</param>
            void Vote(VoteRequest request)
            {
                var lastLogIndex = this.Logs.Count;
                var lastLogTerm = this.GetLogTermForIndex(lastLogIndex);

                if (request.Term < this.CurrentTerm ||
                    (this.VotedFor != null && this.VotedFor != request.CandidateId) ||
                    lastLogIndex > request.LastLogIndex ||
                    lastLogTerm > request.LastLogTerm)
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

            /// <summary>
            /// Processes the given append entries request.
            /// </summary>
            /// <param name="request">AppendEntriesRequest</param>
            void AppendEntries(AppendEntriesRequest request)
            {
                if (request.Term < this.CurrentTerm)
                {
                    this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm, false,
                        this.Id, request.ReceiverEndpoint));
                }
                else
                {
                    if (request.PrevLogIndex > 0 &&
                        (this.Logs.Count < request.PrevLogIndex ||
                        this.Logs[request.PrevLogIndex - 1].Term != request.PrevLogTerm))
                    {
                        this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm,
                            false, this.Id, request.ReceiverEndpoint));
                    }
                    else
                    {
                        if (request.Entries.Count > 0)
                        {
                            var currentIndex = request.PrevLogIndex + 1;
                            foreach (var entry in request.Entries)
                            {
                                if (this.Logs.Count < currentIndex)
                                {
                                    this.Logs.Add(entry);
                                }
                                else if (this.Logs[currentIndex - 1].Term != entry.Term)
                                {
                                    this.Logs.RemoveRange(currentIndex - 1, this.Logs.Count - (currentIndex - 1));
                                    this.Logs.Add(entry);
                                }

                                currentIndex++;
                            }
                        }

                        if (request.LeaderCommit > this.CommitIndex &&
                            this.Logs.Count < request.LeaderCommit)
                        {
                            this.CommitIndex = this.Logs.Count;
                        }
                        else if (request.LeaderCommit > this.CommitIndex)
                        {
                            this.CommitIndex = request.LeaderCommit;
                        }

                        if (this.CommitIndex > this.LastApplied)
                        {
                            this.LastApplied++;
                        }

                        this.LeaderId = request.LeaderId;
                        this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm,
                            true, this.Id, request.ReceiverEndpoint));
                    }
                }
            }

            void RedirectLastClientRequestToClusterManager()
            {
                if (this.LastClientRequest != null)
                {
                    this.Send(this.ClusterManager, this.LastClientRequest);
                }
            }

            /// <summary>
            /// Returns the log term for the given log index.
            /// </summary>
            /// <param name="logIndex">Index</param>
            /// <returns>Term</returns>
            int GetLogTermForIndex(int logIndex)
            {
                var logTerm = 0;
                if (logIndex > 0)
                {
                    logTerm = this.Logs[logIndex - 1].Term;
                }

                return logTerm;
            }

            void ShuttingDown()
            {
                this.Send(this.ElectionTimer, new Halt());
                this.Send(this.PeriodicTimer, new Halt());

                this.Raise(new Halt());
            }
        }

        class Client : Machine
        {
            /// <summary>
            /// Used to configure the client.
            /// </summary>
            public class ConfigureEvent : Event
            {
                public MachineId Cluster;

                public ConfigureEvent(MachineId cluster)
                    : base()
                {
                    this.Cluster = cluster;
                }
            }

            /// <summary>
            /// Used for a client request.
            /// </summary>
            internal class Request : Event
            {
                public MachineId Client;
                public int Command;

                public Request(MachineId client, int command)
                    : base()
                {
                    this.Client = client;
                    this.Command = command;
                }
            }

            internal class Response : Event { }

            private class LocalEvent : Event { }

            MachineId Cluster;

            int LatestCommand;
            int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.LatestCommand = -1;
                this.Counter = 0;
            }

            void Configure()
            {
                this.Cluster = (this.ReceivedEvent as ConfigureEvent).Cluster;
                this.Raise(new LocalEvent());
            }

            [OnEntry(nameof(PumpRequestOnEntry))]
            [OnEventDoAction(typeof(Response), nameof(ProcessResponse))]
            [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
            class PumpRequest : MachineState { }

            void PumpRequestOnEntry()
            {
                this.LatestCommand = this.RandomInteger(100); //new Random().Next(100);
                this.Counter++;
                this.Send(this.Cluster, new Request(this.Id, this.LatestCommand));
            }

            void ProcessResponse()
            {
                if (this.Counter == 3)
                {
                    this.Send(this.Cluster, new ClusterManager.ShutDown());
                    this.Raise(new Halt());
                }
                else
                {
                    this.Raise(new LocalEvent());
                }
            }
        }

        class ElectionTimer : Machine
        {
            internal class ConfigureEvent : Event
            {
                public MachineId Target;

                public ConfigureEvent(MachineId id)
                    : base()
                {
                    this.Target = id;
                }
            }

            internal class StartTimer : Event { }
            internal class CancelTimer : Event { }
            internal class Timeout : Event { }

            private class TickEvent : Event { }

            MachineId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(StartTimer), typeof(Active))]
            class Init : MachineState { }

            void Configure()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimer))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send(this.Id, new TickEvent());
            }

            void Tick()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new Timeout());
                }

                this.Raise(new CancelTimer());
            }

            [OnEventGotoState(typeof(StartTimer), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            class Inactive : MachineState { }
        }

        class PeriodicTimer : Machine
        {
            internal class ConfigureEvent : Event
            {
                public MachineId Target;

                public ConfigureEvent(MachineId id)
                    : base()
                {
                    this.Target = id;
                }
            }

            internal class StartTimer : Event { }
            internal class CancelTimer : Event { }
            internal class Timeout : Event { }

            private class TickEvent : Event { }

            MachineId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(StartTimer), typeof(Active))]
            class Init : MachineState { }

            void Configure()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimer))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send(this.Id, new TickEvent());
            }

            void Tick()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new Timeout());
                }

                this.Raise(new CancelTimer());
            }

            [OnEventGotoState(typeof(StartTimer), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            class Inactive : MachineState { }
        }

        class SafetyMonitor : Monitor
        {
            internal class NotifyLeaderElected : Event
            {
                public int Term;

                public NotifyLeaderElected(int term)
                    : base()
                {
                    this.Term = term;
                }
            }

            private class LocalEvent : Event { }

            private HashSet<int> TermsWithLeader;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Monitoring))]
            class Init : MonitorState { }

            void InitOnEntry()
            {
                this.TermsWithLeader = new HashSet<int>();
                this.Raise(new LocalEvent());
            }

            [OnEventDoAction(typeof(NotifyLeaderElected), nameof(ProcessLeaderElected))]
            class Monitoring : MonitorState { }

            void ProcessLeaderElected()
            {
                var term = (this.ReceivedEvent as NotifyLeaderElected).Term;

                this.Assert(!this.TermsWithLeader.Contains(term), $"Detected more than one leader.");
                this.TermsWithLeader.Add(term);
            }
        }

        [Fact]
        public void TestMultipleLeadersInRaftProtocol()
        {
            var configuration = base.GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 100;
            configuration.MaxFairSchedulingSteps = 1000;
            configuration.LivenessTemperatureThreshold = 500;
            configuration.RandomSchedulingSeed = 495;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(SafetyMonitor));
                r.CreateMachine(typeof(ClusterManager));
            });

            var bugReport = "Detected more than one leader.";
            base.AssertFailed(configuration, test, bugReport);
        }
    }
}
