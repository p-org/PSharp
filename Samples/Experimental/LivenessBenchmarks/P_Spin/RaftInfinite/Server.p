#include "electionTimer.p"

event ServerConfigureEvent : (int, seq[machine], machine);
event VoteRequest : (int, machine);
event VoteResponse : (int, bool);
event BecomeFollower;
event BecomeCandidate;
event BecomeLeader;
event ShutDown;
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
		on ServerConfigureEvent do (payload : (int, seq[machine], machine))
		{	
			ServerId = payload.0;
            Servers = payload.1;
            ClusterManager = payload.2;
			ElectionTimer = new ElectionTimer();
            send ElectionTimer, ConfigureEvent, this;
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
			send ElectionTimer, StartTimer;
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
		on Timeout do 
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
			CurrentTerm = CurrentTerm + 1;
            VotedFor = this;
            VotesReceived = 1;
			send ElectionTimer, StartTimer;
			BroadcastVoteRequests();			
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
                if (VotesReceived >= ((sizeof(Servers) / 2) + 1))
                {
                    VotesReceived = 0;
                    raise BecomeLeader;
                }
            }
		}
		on Timeout do
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
		ignore Timeout;
	}

	fun BroadcastVoteRequests()
	{
		var idx : int;

		idx = 0;
		while (idx < sizeof(Servers))
		{
			if (idx == ServerId){}
            send Servers[idx], VoteRequest, CurrentTerm, this;
			idx = idx + 1;
		}
	}

	fun Vote(Term : int, CandidateId : machine)
	{
		if (Term < CurrentTerm || (VotedFor != null && VotedFor != CandidateId))
        {
            send CandidateId, VoteResponse, CurrentTerm, false;
        }
        else
        {
            VotedFor = CandidateId;
            LeaderId = null;
			send CandidateId, VoteResponse, CurrentTerm, true;
        }
	}
}

spec liveness observes NotifyLeaderElected
{
	start hot state LeaderNotElected
	{
		on NotifyLeaderElected goto LeaderElected;
	}

	cold state LeaderElected
	{
		on NotifyLeaderElected goto LeaderElected;
	}
}