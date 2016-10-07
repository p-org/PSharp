event ConfigureEvent : (int, seq[machine], machine);
event VoteRequest : (int, machine, int, int);
event VoteResponse : (int, bool);
event AppendEntriesRequest : (int, machine, int, int, seq[Log], int, machine);
event AppendEntriesResponse : (int, bool, machine, machine);
event BecomeFollower;
event BecomeCandidate;
event BecomeLeader;
event ShutDown;

machine Server
{
	var ServerId : int;
	var ClusterManager : machine;
	var Servers : seq[machine];
	var LeaderId : machine;
	var ElectionTimer : machine;
	var PeriodicTimer : machine;
	var CurrentTerm : int;
	var VotedFor : machine;
	var Logs : seq[Log];
	var CommitIndex : int;
	var LastApplied : int;
	var NextIndex : map[machine, int];
	var MatchIndex : map[machine, int];
	var VotesReceived : int;
	var LastClientRequest : Client_Request;

	start state Init 
	{
		entry
		{
			CurrentTerm = 0;

            LeaderId = null;
            VotedFor = null;

            Logs = default(seq[Log]);

            CommitIndex = 0;
            LastApplied = 0;

            NextIndex = default(map[machine, int]);
            MatchIndex = default(map[machine, int]);
		}
		on ConfigureEvent do (payload : ((int, seq[machine], machine)))
		{
			ServerId = payload.0;
            Servers = payload.1;
            ClusterManager = payload.2;

            ElectionTimer = new ElectionTimer();
            Send ElectionTimer, ElectionTimer_ConfigureEvent, this;

            PeriodicTimer = new PeriodicTimer();
            Send PeriodicTimer, PeriodicTimer_ConfigureEvent, this;

            raise BecomeFollower;
		}
		on BecomeFollower goto Follower;
		defer VoteRequest, AppendEntriesRequest;
	}

	state Follower
	{
		entry
		{
			LeaderId = null;
            VotesReceived = 0;

            send ElectionTimer, ElectionTimer_StartTimer;
		}
		on Client_Request do (payload : (machine, int))
		{
			if (LeaderId != null)
            {
                send LeaderId, Client_Request, payload.0, payload.1;
            }
            else
            {
                send ClusterManager, ClusterManager_RedirectRequest, payload.0, payload.1;
            }
		}
		on ElectionTimer_Timeout do
		{
			this.Raise(new BecomeCandidate());
		}
		on VoteRequest do (request : (int, machine, int, int))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = null;
            }

            Vote(request);
		}
		on VoteResponse do (request : (int, bool))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
            }
		}
		on AppendEntriesRequest do (request : (int, machine, int, int, seq[Log], int, machine))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
            }

            AppendEntries(request);
		}
		on AppendEntriesResponse do (request : (int, bool, machine, machine))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
            }
		}
		on ShutDown do 
		{
			send ElectionTimer, halt;
            send PeriodicTimer, halt;

            raise halt;
		}
		on BecomeFollower goto Follower;
		on BecomeCandidate goto Candidate;
		ignore PeriodicTimer_Timeout;
	}

	state Candidate
	{
		entry
		{
			CurrentTerm = CurrentTerm + 1;
            VotedFor = this;
            VotesReceived = 1;

            send ElectionTimer, ElectionTimer_StartTimer;
            BroadcastVoteRequests();
		}
		on Client_Request do 
		{
			if (LeaderId != null)
            {
                send LeaderId, Client_Request, payload.0, payload.1;
            }
            else
            {
                send ClusterManager, ClusterManager_RedirectRequest, payload.0, payload.1;
            }
		}
		on VoteRequest do (request : (int, machine, int, int))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
                Vote(request);
                raise BecomeFollower;
            }
            else
            {
                Vote(request);
            }
		}
		on VoteResponse do (request : (int, bool))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
                raise BecomeFollower;
            }
            else if (request.0 != CurrentTerm)
            {
                return;
            }

            if (request.1)
            {
                VotesReceived = VotesReceived + 1;
                if (VotesReceived >= (sizeof(Servers) / 2) + 1)
                {
                    VotesReceived = 0;
                    raise BecomeLeader;
                }
            }
		}
		on AppendEntriesRequest do (request : (int, machine, int, int, seq[Log], int, machine))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
                AppendEntries(request);
                raise BecomeFollower;
            }
            else
            {
                AppendEntries(request);
            }
		}
		on AppendEntriesResponse do (request : (int, bool, machine, machine))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;
                raise BecomeFollower;
            }
		}
		on ElectionTimer_Timeout do 
		{
			raise BecomeCandidate;
		}
		on PeriodicTimer.Timeout do 
		{
			var index : int;
			var lastLogIndex : int;
			var lastLogTerm : int

			index = 0;
			while (index < sizeof(Servers))
			{	
				if (index == ServerId)
                    continue;

				lastLogIndex = sizeof(Logs);
                lastLogTerm = GetLogTermForIndex(lastLogIndex);

                send Servers[index], VoteRequest, CurrentTerm, this, lastLogIndex, lastLogTerm;
				index = index  + 1;
			}
		}
		on ShutDown do 
		{
			send ElectionTimer, halt;
            send PeriodicTimer, halt;

            raise halt;
		}
		on BecomeLeader goto Leader;
		on BecomeFollower goto Follower;
		on BecomeCandidate goto Candidate;
	}

	state Leader
	{
		entry
		{
			var logIndex : int;
			var logTerm : int;
			var index : int;
			var LogList : seq[Log];

			announce SafetyMonitor_NotifyLeaderElected, CurrentTerm;
            announce LivenessMonitor_NotifyLeaderElected;
            send ClusterManager, ClusterManager_NotifyLeaderUpdate, this, CurrentTerm;

            logIndex = sizeof(Logs);
            logTerm = GetLogTermForIndex(logIndex);

            NextIndex = default(map[machine, int]);
            MatchIndex = default(map[machine, int]);

			index = 0;
			while (index < sizeof(Servers))
			{
				if (idx == ServerId)
                    continue;
                NextIndex += (Servers[index], (logIndex + 1));
                MatchIndex + (Servers[index], 0);
				index = index + 1;
			}

			index = 0;
			while (index < sizeof(Servers))
			{
				if (index == ServerId)
                    continue;

				LogList = default(seq[Log]);
                send(Servers[index], AppendEntriesRequest, CurrentTerm, this, logIndex, logTerm, LogList, CommitIndex, null));
				index = index + 1;
			}
		}
		on Client_Request do (payload : (machine, int))
		{
            var log : Log;

			LastClientRequest = payload;
			log = default(Log);
			log.CurrentTerm = CurrentTerm;
			log.Command = LastClientRequest.1);
            Logs += (sizeof(Logs), log);

            BroadcastLastClientRequest();
		}
		on VoteRequest do (request : (int, machine, int, int))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;

                RedirectLastClientRequestToClusterManager();
                Vote(request);

                raise BecomeFollower;
            }
            else
            {
                Vote(request);
            }
		}
		on VoteResponse do (request : (int, bool))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;

                RedirectLastClientRequestToClusterManager();
                raise BecomeFollower;
            }
		}
		on AppendEntriesRequest do (request : (int, machine, int, int, seq[Log], int, machine))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
                VotedFor = null;

                RedirectLastClientRequestToClusterManager();
                AppendEntries(request);

                raise BecomeFollower;
            }
		}
		on AppendEntriesResponse do (request : (int, bool, machine, machine))
		{
			var commitIndex : int;
			var logs : seq[Log];
			var index : int;
			var prevLogIndex : int;
			var prevLogTerm : int;

            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = null;

                RedirectLastClientRequestToClusterManager();
                raise BecomeFollower;
            }
            else if (request.0 != CurrentTerm)
            {
                return;
            }

            if (request.1)
            {
                NextIndex[request.2] = sizeof(Logs) + 1;
                MatchIndex[request.2] = sizeof(Logs);

                VotesReceived = VotesReceived + 1;
                if (request.3 != null &&
                    VotesReceived >= (sizeof(Servers) / 2) + 1)
                {
                    commitIndex = MatchIndex[request.2];
                    if (commitIndex > CommitIndex && Logs[commitIndex - 1].Term == CurrentTerm)
                    {
                        CommitIndex = commitIndex;
                    }

                    VotesReceived = 0;
                    LastClientRequest = null;

                    send request.3, Client_Response;
                }
            }
            else
            {
                if (NextIndex[request.2] > 1)
                {
                    NextIndex[request.2] = NextIndex[request.2] - 1;
                }

				logs = default(seq[Log]);
				index = NextIndex[request.2] - 1
				while (index <= (sizeof(Logs) - NextIndex[request.2] - 1))
				{
					logs += (sizeof(logs), Logs[index]);
					index = index + 1;
				}

                prevLogIndex = NextIndex[request.2] - 1;
                prevLogTerm = GetLogTermForIndex(prevLogIndex);

                send request.2, AppendEntriesRequest, CurrentTerm, this, prevLogIndex, prevLogTerm, logs, CommitIndex, request.3;
            }
		}
		on ShutDown do 
		{
			send ElectionTimer, halt;
            send PeriodicTimer, halt;

            raise halt;
		}
		on BecomeFollower goto Follower;
		ignore ElectionTimer_Timeout, PeriodicTimer_Timeout;
	}

	fun BroadcastLastClientRequest()
    {
        var lastLogIndex : int;
		var index : int;
		var server : machine;
		var logs : seq[Log];
		var cIndex : int;
		var prevLogIndex : int;
		var prevLogTerm : int;

		lastLogIndex = sizeof(Logs);
        VotesReceived = 1;
            
		index = 0;
		while (index < sizeof(Servers))
		{
            if (index == ServerId)
                continue;

            server = Servers[index];
            if (lastLogIndex < NextIndex[server])
                continue;

			cIndex = NextIndex[server] - 1;
			logs = default(seq[Log]);
			while (cIndex <= sizeof(Logs) - NextIndex[server] - 1)
			{	
				logs += (sizeof(logs), Logs[cIndex]);
				cIndex = cIndex + 1;
			}
            prevLogIndex = NextIndex[server] - 1;
            prevLogTerm = GetLogTermForIndex(prevLogIndex);

            send server, AppendEntriesRequest, CurrentTerm, this, prevLogIndex, prevLogTerm, logs, CommitIndex, this.LastClientRequest.0));
			index = index + 1;
        }
    }
}

namespace Raft
{
    /// <summary>
    /// A server in Raft can be one of the following three roles:
    /// follower, candidate or leader.
    /// </summary>
    internal class Server : Machine
    {
        

        #endregion

        #region general methods

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
                Console.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm +
                    " | log " + this.Logs.Count + " | vote false\n");
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, false));
            }
            else
            {
                Console.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm +
                    " | log " + this.Logs.Count + " | vote true\n");

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
                Console.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                    this.Logs.Count + " | last applied: " + this.LastApplied + " | append false (< term)\n");

                this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm, false,
                    this.Id, request.ReceiverEndpoint));
            }
            else
            {
                if (request.PrevLogIndex > 0 &&
                    (this.Logs.Count < request.PrevLogIndex ||
                    this.Logs[request.PrevLogIndex - 1].Term != request.PrevLogTerm))
                {
                    Console.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                        this.Logs.Count + " | last applied: " + this.LastApplied + " | append false (not in log)\n");

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

                    Console.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                        this.Logs.Count + " | entries received " + request.Entries.Count + " | last applied " +
                        this.LastApplied + " | append true\n");

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

        #endregion
    }
}
