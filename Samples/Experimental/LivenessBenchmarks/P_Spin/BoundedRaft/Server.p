event ConfigureEvent : (int, seq[machine], machine);
event VoteRequest : (int, machine, int, int);
event VoteResponse : (int, bool); 
event BecomeFollower;
event BecomeCandidate;
event BecomeLeader;
event NotifyLeaderElected;
event AppendEntriesRequest : (int, machine, int, int, seq[Log], int, machine);
event AppendEntriesResponse : (int, bool, machine, machine);

type Log = (Term : int, Command : int);

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
	var VotesReceived : int;
	var NextIndex : map[machine, int];
	var MatchIndex : map[machine, int];
	var LastClientRequest : (machine, int);

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

			LastClientRequest.0 = null;
		}
		on ConfigureEvent do (payload : (int, seq[machine], machine))
		{
			ServerId = payload.0;
            Servers = payload.1;
            ClusterManager = payload.2;

            ElectionTimer = new ElectionTimer();
            send ElectionTimer, ElectionTimer_ConfigureEvent, this;

			PeriodicTimer = new PeriodicTimer();
            send PeriodicTimer, PeriodicTimer_ConfigureEvent, this;

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
			announce LeaderElectionStarted;
            send ElectionTimer, ElectionTimer_StartTimer;
		}
		on VoteRequest do (payload : (int, machine, int, int))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
            }

            Vote(payload.0, payload.1, payload.2, payload.3);
		}
		on VoteResponse do (payload : (int, bool))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
            }
		}
		on ElectionTimer_Timeout do 
		{
			raise BecomeCandidate;
		}
		on ShutDown do 
		{
			send ElectionTimer, halt;
			send PeriodicTimer, halt;
            raise halt;
		}
		on Request do (payload : (machine, int))
		{
			if (LeaderId != null)
            {
                send LeaderId, Request, payload.0, payload.1;
            }
            else
            {
                send ClusterManager, RedirectRequest, payload.0, payload.1;
            }
		}
		on AppendEntriesRequest do (payload : (int, machine, int, int, seq[Log], int, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
            }

            AppendEntries(payload.0, payload.1, payload.2, payload.3, payload.4, payload.5, payload.6);
		}
		on AppendEntriesResponse do (payload : (int, bool, machine, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
            }
		}
		on BecomeFollower goto Follower;
		on BecomeCandidate goto Candidate; 
		ignore PeriodicTimer_Timeout;
	}

	state Candidate
	{
		entry
		{
			CurrentTerm = CurrentTerm + 1;;
            VotedFor = this;
            VotesReceived = 1;

            send ElectionTimer, ElectionTimer_StartTimer;
			BroadcastVoteRequests();
		}
		on VoteRequest do (payload : (int, machine, int, int))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
                Vote(payload.0, payload.1, payload.2, payload.3);
                raise BecomeFollower;
            }
            else
            {
                Vote(payload.0, payload.1, payload.2, payload.3);
            }
		}
		on VoteResponse do (payload : (int, bool))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
                raise BecomeFollower;
            }
            else if (payload.0 != CurrentTerm)
            {
                return;
            }

            if (payload.1)
            {
                VotesReceived = VotesReceived + 1;
                if (VotesReceived >= (sizeof(Servers) / 2) + 1)
                {
                    VotesReceived = 0;
                    raise BecomeLeader;
                }
            }
		}
		on Request do (payload : (machine, int))
		{
			if (LeaderId != null)
            {
                send LeaderId, Request, payload.0, payload.1;
            }
            else
            {
                send ClusterManager, RedirectRequest, payload.0, payload.1;
            }
		}
		on ElectionTimer_Timeout do 
		{
			raise BecomeCandidate;
		}
		on PeriodicTimer_Timeout do
		{	
			BroadcastVoteRequests();
		}
		on AppendEntriesRequest do (payload : (int, machine, int, int, seq[Log], int, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
                AppendEntries(payload.0, payload.1, payload.2, payload.3, payload.4, payload.5, payload.6);
                raise BecomeFollower;
            }
            else
            {
                AppendEntries(payload.0, payload.1, payload.2, payload.3, payload.4, payload.5, payload.6);
            }
		}
		on AppendEntriesResponse do (payload : (int, bool, machine, machine))
		{
			if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
                raise BecomeFollower;
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

            announce NotifyLeaderElected;
            send ClusterManager, NotifyLeaderUpdate, this, CurrentTerm;

            logIndex = sizeof(Logs);
            logTerm = GetLogTermForIndex(logIndex);

			index = 0;
			while(index < sizeof(NextIndex))
			{
				NextIndex -= keys(NextIndex)[index];
				index = index + 1;
			}
			index = 0;
			while(index < sizeof(MatchIndex))
			{
				MatchIndex -= keys(MatchIndex)[index];
				index = index + 1;
			}

			index = 0;
			while(index < sizeof(Servers))
			{
				if (index == ServerId){}
				else
				{
					NextIndex[Servers[index]] = logIndex + 1;
					MatchIndex[Servers[index]] = 0;
				}
				index = index + 1;
			}

			index = 0;
			while (index < sizeof(Servers))
			{
				if (index == ServerId){}
				else
				{
					LogList = default(seq[Log]);
					send Servers[index], AppendEntriesRequest, CurrentTerm, this, logIndex, logTerm, LogList, CommitIndex, null;
				}
				index = index + 1;
			}
		}
		on Request do (payload : (machine, int))
		{
			var log : Log;
			var lastLogIndex : int;
			var index : int;
			var server : machine;
			var logs : seq[Log];
			var prevLogIndex : int;
			var prevLogTerm : int;
			var tIndex : int;

			LastClientRequest.0 = payload.0;
			LastClientRequest.1 = payload.1;

            log = default(Log);
			log.Term = CurrentTerm;
			log.Command =  LastClientRequest.1;
            Logs += (sizeof(Logs), log);
			
            lastLogIndex = sizeof(Logs);
            VotesReceived = 1;

			index = 0;
			while (index < sizeof(Servers))
			{
				if (index == ServerId){}
                else
				{
					server = Servers[index];
					if (lastLogIndex < NextIndex[server]){}
                    else
					{
						tIndex = NextIndex[server] - 1;
						while (tIndex > 0 && tIndex <= (sizeof(Logs) - (NextIndex[server] - 1)))
						{
							logs += (sizeof(logs), Logs[index]);
							tIndex = tIndex + 1;
						}

						prevLogIndex = NextIndex[server] - 1;
						prevLogTerm = GetLogTermForIndex(prevLogIndex);
						
                        send server, AppendEntriesRequest, CurrentTerm, this, prevLogIndex, prevLogTerm, logs, CommitIndex, LastClientRequest.0;
					
					}
				}
				index = index + 1;
			}
		}
		on VoteRequest do (payload : (int, machine, int, int))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;

				RedirectLastClientRequestToClusterManager();
                Vote(payload.0, payload.1, payload.2, payload.3);

                raise BecomeFollower;
            }
            else
            {
                Vote(payload.0, payload.1, payload.2, payload.3);
            }
		}
		on VoteResponse do (payload : (int, bool))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;

				RedirectLastClientRequestToClusterManager();
                raise BecomeFollower;
            }
		}
		on AppendEntriesRequest do (payload : (int, machine, int, int, seq[Log], int, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;

                RedirectLastClientRequestToClusterManager();
                AppendEntries(payload.0, payload.1, payload.2, payload.3, payload.4, payload.5, payload.6);

                raise BecomeFollower;
            }
		}
		on AppendEntriesResponse do (payload : (int, bool, machine, machine))
		{
			var commitIndex : int;
			var logs : seq[Log];
			var index : int;
			var prevLogIndex : int;
			var prevLogTerm : int;

            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;

                RedirectLastClientRequestToClusterManager();
                raise BecomeFollower;
            }
            else if (payload.0 != CurrentTerm)
            {
                return;
            }

            if (payload.1)
            {
                NextIndex[payload.2] = sizeof(Logs) + 1;
                MatchIndex[payload.2] = sizeof(Logs);

                VotesReceived = VotesReceived + 1;
                if (payload.3 != null && VotesReceived >= (sizeof(Servers) / 2) + 1)
                {
                    commitIndex = MatchIndex[payload.2];
                    if (commitIndex > CommitIndex && Logs[commitIndex - 1].Term == CurrentTerm)
                    {
                        CommitIndex = commitIndex;
                    }

                    VotesReceived = 0;
                    LastClientRequest.0 = null;

                    send payload.3, Response;
                }
            }
            else
            {
                if (NextIndex[payload.2] > 1)
                {
                    NextIndex[payload.2] = NextIndex[payload.2] - 1;
                }

				index = NextIndex[payload.2] - 1;
				logs = default(seq[Log]);
				while (index < sizeof(Logs) - (NextIndex[payload.2] - 1))
				{
					logs += (sizeof(logs), Logs[index]);
					index = index + 1;
				}

                prevLogIndex = NextIndex[payload.2] - 1;
                prevLogTerm = GetLogTermForIndex(prevLogIndex);

                send payload.2, AppendEntriesRequest, CurrentTerm, this, prevLogIndex, prevLogTerm, logs, CommitIndex, payload.3;
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

	fun Vote(term : int, candidateId : machine, LastLogIndex : int, LastLogTerm : int)
    {
		var lastLogIndex : int;
		var lastLogTerm  : int;

		lastLogIndex = sizeof(Logs);
		lastLogTerm = GetLogTermForIndex(lastLogIndex);
		if (term < CurrentTerm || (VotedFor != null && VotedFor != candidateId) || lastLogIndex > LastLogIndex || lastLogTerm > LastLogTerm)
        {
            send candidateId, VoteResponse, CurrentTerm, false;
        }
        else
        {
            VotedFor = candidateId;
            LeaderId = null;
			
			send candidateId, VoteResponse, CurrentTerm, true;
        }
    }

	fun AppendEntries(Term : int, rLeaderId : machine, PrevLogIndex : int, PrevLogTerm : int, Entries : seq[Log], LeaderCommit : int, ReceiverEndpoint : machine)
    {
		var currentIndex : int;
		var index : int;
		var entryVal : Log;
		var tIndex : int;
		var tLog : Log;

        if (Term < CurrentTerm)
        {
            send rLeaderId, AppendEntriesResponse, CurrentTerm, false, this, ReceiverEndpoint;
        }
        else
        {
            if (PrevLogIndex > 0)
            {
				if((sizeof(Logs) < PrevLogIndex) || (Logs[PrevLogIndex - 1].Term != PrevLogTerm))
				{
					send rLeaderId, AppendEntriesResponse, CurrentTerm, false, this, ReceiverEndpoint;
				}
				else
				{
					if (sizeof(Entries) > 0)
                {
                    currentIndex = PrevLogIndex + 1;
					
					index = 0;
					while (sizeof(Entries) > 0 && index < sizeof(Entries))
					{
						entryVal = Entries[index];
						if (sizeof(Logs) < currentIndex)
                        {
                            Logs += (sizeof(Logs), entryVal);
                        }
                        else if (currentIndex > 0 && sizeof(Logs) > 0 && Logs[currentIndex - 1].Term != entryVal.Term)
                        {
							tIndex = currentIndex - 1;
							while (tIndex <= (sizeof(Logs) - (currentIndex - 1)))
							{	
								if(tIndex >= 0  && tIndex < sizeof(Logs))
								{
									Logs -= tIndex;
								}
								tIndex = tIndex + 1;
							}
                            Logs += (sizeof(Logs), entryVal);
                        }

                        currentIndex = currentIndex + 1;
						index = index + 1;
					}
                }

                if (LeaderCommit > CommitIndex && sizeof(Logs) < LeaderCommit)
                {
                    CommitIndex = sizeof(Logs);
                }
                else if (LeaderCommit > CommitIndex)
                {
                    CommitIndex = LeaderCommit;
                }

                if (CommitIndex > LastApplied)
                {
                    LastApplied = LastApplied + 1;
                }
				
				LeaderId = rLeaderId;
                send LeaderId, AppendEntriesResponse, CurrentTerm, true, this, ReceiverEndpoint;
				}  
            }
        }
    }
	fun BroadcastVoteRequests()
	{
		var index : int;
		var lastLogIndex : int;
		var lastLogTerm : int;

		index = 0;
		while (index < sizeof(Servers))
		{	
			if (index == ServerId){}
			else
			{
				lastLogIndex = sizeof(Logs);
                lastLogTerm = GetLogTermForIndex(lastLogIndex);
				send Servers[index], VoteRequest, CurrentTerm, this, lastLogIndex, lastLogTerm;
			}
			index = index + 1;
		}
	}
	fun GetLogTermForIndex(logIndex : int) : int
    {
        var logTerm : int;
		logTerm = 0;
        if (logIndex > 0)
        {
            logTerm = Logs[logIndex - 1].Term;
        }

        return logTerm;
    }
	fun RedirectLastClientRequestToClusterManager()
    {
        if (LastClientRequest.0 != null)
        {
            send ClusterManager, Request, LastClientRequest.0, LastClientRequest.1;
        }
    }
}