event ConfigureEvent : (int, seq[machine], machine);
event VoteRequest : (int, machine);
event VoteResponse : (int, bool); 
event BecomeFollower;
event BecomeCandidate;
event BecomeLeader;
event NotifyLeaderElected;

machine Server
{
	var ServerId : int;
	var ClusterManager : machine;
	var Servers : seq[machine];
	var LeaderId : machine;
	var ElectionTimer : machine;
	var CurrentTerm : int;
	var VotedFor : machine;
	var CommitIndex : int;
	var LastApplied : int;
	var VotesReceived : int;

	start state Init
	{	
		entry
		{
			CurrentTerm = 0;
            LeaderId = null;
            VotedFor = null;
            CommitIndex = 0;
            LastApplied = 0;
		}
		on ConfigureEvent do (payload : (int, seq[machine], machine))
		{
			ServerId = payload.0;
            Servers = payload.1;
            ClusterManager = payload.2;

            ElectionTimer = new ElectionTimer();
            send ElectionTimer, ElectionTimer_ConfigureEvent, this;
            raise BecomeFollower;
		}
		on BecomeFollower goto Follower;
		defer VoteRequest;
	}

	state Follower
	{
		entry
		{
			LeaderId = null;
            VotesReceived = 0;

            send ElectionTimer, ElectionTimer_StartTimer;
		}
		on VoteRequest do (payload : (int, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
            }

            Vote(payload.0, payload.1);
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
            raise halt;
		}
		on BecomeFollower goto Follower;
		on BecomeCandidate goto Candidate;
	}

	state Candidate
	{
		entry
		{
			var index : int;

			CurrentTerm = CurrentTerm + 1;;
            VotedFor = this;
            VotesReceived = 1;

            send ElectionTimer, ElectionTimer_StartTimer;

			index = 0;
			while (index < sizeof(Servers))
			{	
				if (index == ServerId){}
				else
				{
					send Servers[index], VoteRequest, CurrentTerm, this;
				}
				index = index + 1;
			}
		}
		on VoteRequest do (payload : (int, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;
                Vote(payload.0, payload.1);
                raise BecomeFollower;
            }
            else
            {
                Vote(payload.0, payload.1);
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
		on ElectionTimer_Timeout do 
		{
			raise BecomeCandidate;
		}
		on ShutDown do 
		{
			send ElectionTimer, halt;
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
            announce NotifyLeaderElected;
            send ClusterManager, NotifyLeaderUpdate, this, CurrentTerm;
		}
		on VoteRequest do (payload : (int, machine))
		{
            if (payload.0 > CurrentTerm)
            {
                CurrentTerm = payload.0;
                VotedFor = null;

                Vote(payload.0, payload.1);

                raise BecomeFollower;
            }
            else
            {
                Vote(payload.0, payload.1);
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
		}
		on ShutDown do 
		{
			send ElectionTimer, halt;
            raise halt;
		}
		on BecomeFollower goto Follower;
		ignore ElectionTimer_Timeout;
	}

	fun Vote(term : int, candidateId : machine)
    {
		if (term < CurrentTerm || (VotedFor != null && VotedFor != candidateId))
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
}

