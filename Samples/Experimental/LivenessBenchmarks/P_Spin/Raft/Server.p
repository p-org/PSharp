#include "ElectionTimer.p"
#include "PeriodicTimer.p"
#include "Client.p"

event Server_ConfigureEvent : (int, seq[machine], machine);
event VoteRequest : (int, machine, int, int);
event VoteResponse : (int, bool);
event AppendEntriesRequest : (int, machine, int, int, seq[Log], int, machine);
event AppendEntriesResponse : (int, bool, machine, machine);
event BecomeFollower;
event BecomeCandidate;
event BecomeLeader;
event ShutDown;

event LivenessMonitor_NotifyLeaderElected;

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
	var NextIndex : map[machine, int];
	var MatchIndex : map[machine, int];
	var VotesReceived : int;
	var LastClientRequestMachine : machine;
	var LastClientRequestInt : int;

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

			LastClientRequestMachine = null;
			LastClientRequestInt = -1;

			Servers = default(seq[machine]);
		}
		on Server_ConfigureEvent do (payload : (int, seq[machine], machine))
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
                send ClusterManager, RedirectRequest, payload.0, payload.1;
            }
		}
		on ElectionTimer_Timeout do
		{
			raise BecomeCandidate;
		}
		on VoteRequest do (request : (int, machine, int, int))
		{
            if (request.0 > CurrentTerm)
            {
                CurrentTerm = request.0;
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
		on Client_Request do (payload : (machine, int))
		{
			if (LeaderId != null)
            {
                send LeaderId, Client_Request, payload.0, payload.1;
            }
            else
            {
                send ClusterManager, RedirectRequest, payload.0, payload.1;
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
		on PeriodicTimer_Timeout do 
		{
			var index : int;
			var lastLogIndex : int;
			var lastLogTerm : int;

			index = 0;
			while (index < sizeof(Servers))
			{	
				if (index == ServerId)
				{
					
				}
				
				else
				{
					lastLogIndex = sizeof(Logs);
					lastLogTerm = GetLogTermForIndex(lastLogIndex);

					send Servers[index], VoteRequest, CurrentTerm, this, lastLogIndex, lastLogTerm;
				}
				
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

            announce LivenessMonitor_NotifyLeaderElected;
            send ClusterManager, NotifyLeaderUpdate, this, CurrentTerm;

            logIndex = sizeof(Logs);
            logTerm = GetLogTermForIndex(logIndex);

            NextIndex = default(map[machine, int]);
            MatchIndex = default(map[machine, int]);

			index = 0;
			while (index < sizeof(Servers))
			{
				if (index == ServerId)
				{

				}
				else
				{
					NextIndex += (Servers[index], (logIndex + 1));
					MatchIndex += (Servers[index], 0);
				}

				index = index + 1;
			}

			index = 0;
			while (index < sizeof(Servers))
			{
				if (index == ServerId)
				{
				}
				else
				{
					LogList = default(seq[Log]);
					send Servers[index], AppendEntriesRequest, CurrentTerm, this, logIndex, logTerm, LogList, CommitIndex, null;
				}
				index = index + 1;
			}
		}
		on Client_Request do (payload : (machine, int))
		{
            var log : Log;

			LastClientRequestMachine = payload.0;
			LastClientRequestInt = payload.1;
			log = default(Log);
			log.Term = CurrentTerm;
			log.Command = LastClientRequestInt;
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
                CurrentTerm = request.0;
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
                    LastClientRequestMachine = null;
					LastClientRequestInt = -1;

                    send request.3, Response;
                }
            }
            else
            {
                if (NextIndex[request.2] > 1)
                {
                    NextIndex[request.2] = NextIndex[request.2] - 1;
                }

				logs = default(seq[Log]);
				index = NextIndex[request.2] - 1;
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
			{
			}
			else
			{
				server = Servers[index];
				if (lastLogIndex < NextIndex[server])
				{
				}
				else
				{
					cIndex = NextIndex[server] - 1;
					logs = default(seq[Log]);
					while (cIndex <= sizeof(Logs) - NextIndex[server] - 1)
					{	
						logs += (sizeof(logs), Logs[cIndex]);
						cIndex = cIndex + 1;
					}
					prevLogIndex = NextIndex[server] - 1;
					prevLogTerm = GetLogTermForIndex(prevLogIndex);
	
					send server, AppendEntriesRequest, CurrentTerm, this, prevLogIndex, prevLogTerm, logs, CommitIndex, LastClientRequestMachine;
				}
			}
            
			index = index + 1;
        }
    }

	fun Vote(request : (int, machine, int, int))
    {
        var lastLogIndex : int;
		var lastLogTerm : int;

		lastLogTerm = GetLogTermForIndex(lastLogIndex);
		lastLogIndex = sizeof(Logs);
        
        if (request.0 < CurrentTerm ||
            (VotedFor != null && VotedFor != request.1) ||
            lastLogIndex > request.2 ||
            lastLogTerm > request.3)
        {
            send request.1, VoteResponse, CurrentTerm, false;
        }
        else
        {
            VotedFor = request.1;
            LeaderId = null;

            send request.1, VoteResponse, CurrentTerm, true;
        }
    }

	fun AppendEntries(request : (int, machine, int, int, seq[Log], int, machine))
    {
		var currentIndex  : int;
		var index : int;
		var rIndex : int;
		var entryVal : Log;

        if (request.0 < CurrentTerm)
        {
           send request.1, AppendEntriesResponse, CurrentTerm, false, this, request.6;
        }
        else
        {
			print "Caught: {0}", request.2;
            if (request.2 > 0 && (sizeof(Logs) < request.2 ||
                (request.2 > 0 && request.2 < sizeof(Logs) && Logs[request.2 - 1].Term != request.3)))
            {
					send request.1, AppendEntriesResponse, CurrentTerm, false, this, request.6;
            }
            else
            {
                if (sizeof(request.4) > 0)
                {
					currentIndex = request.2 + 1;
					
					index = 0;
					while (index < sizeof(request.4))
					{	
						entryVal = request.4[index];
						if (sizeof(Logs) < currentIndex)
                        {
                            Logs += (sizeof(Logs), entryVal);
                        }
                        else if (Logs[currentIndex - 1].Term != entryVal.Term)
                        {
							rIndex = currentIndex - 1;
							while (rIndex < (sizeof(Logs) - (currentIndex - 1)))
							{
								Logs -= rIndex;
								rIndex = rIndex + 1;
							}
                            Logs += (sizeof(Logs), entryVal);
                        }
                        currentIndex = currentIndex + 1;
						index = index + 1;
					}
                }
                if (request.5 > CommitIndex && sizeof(Logs) < request.5)
                {
                    CommitIndex = sizeof(Logs);
                }
                else if (request.5 > CommitIndex)
                {
                    CommitIndex = request.5;
                }

                if (CommitIndex > LastApplied)
                {
                    LastApplied = LastApplied + 1;
                }

				LeaderId = request.1;
                send request.1, AppendEntriesResponse, CurrentTerm, true, this, request.6;
            }
        }
    }

	fun RedirectLastClientRequestToClusterManager()
    {
        if (LastClientRequestMachine != null)
        {
            send ClusterManager, Client_Request, LastClientRequestMachine, LastClientRequestInt;
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

	fun BroadcastVoteRequests()
	{
		var index : int;
		var lastLogIndex : int;
		var lastLogTerm : int;

		index = 0;
		while (index < sizeof(Servers))
		{	
			if (index == ServerId)
			{
					
			}
				
			else
			{
				lastLogIndex = sizeof(Logs);
				lastLogTerm = GetLogTermForIndex(lastLogIndex);

				send Servers[index], VoteRequest, CurrentTerm, this, lastLogIndex, lastLogTerm;
			}
				
			index = index  + 1;
		}
	}
}
