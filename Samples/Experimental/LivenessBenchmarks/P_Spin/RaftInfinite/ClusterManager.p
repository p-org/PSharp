#include "Server.p"

event NotifyLeaderUpdate : (machine, int);
event LocalEvent;

machine Main
{
	var Servers : seq[machine];
	var NumberOfServers : int;
	var Leader : machine;
	var LeaderTerm : int;

	start state Init
	{
		entry
		{
			var idx : int;
			var serv : machine;

			NumberOfServers = 3;
            LeaderTerm = 0;
			Servers = default(seq[machine]);

			idx = 0;
			while (idx < NumberOfServers)
			{
				serv = new Server();
				Servers += (idx, serv);
				idx = idx + 1;
			}
            raise LocalEvent;
		}
		on LocalEvent goto Configuring;
	}

	state Configuring
	{
		entry
		{
			var idx : int;

			idx = 0;
			while (idx < NumberOfServers)
			{	
				send Servers[idx], ServerConfigureEvent, idx, Servers, this;
				idx = idx + 1;
			}
            raise LocalEvent;
		}
		on LocalEvent goto Unavailable;
	}

	state Unavailable
	{
		on NotifyLeaderUpdate do (payload : (machine, int))
		{
			UpdateLeader(payload.0, payload.1);
            raise LocalEvent;			
		}
		on ShutDown do
		{
			var idx : int;

			idx = 0;
			while (idx < NumberOfServers)
			{
				send Servers[idx], ShutDown;
				idx = idx + 1;
			}
			raise halt;
		}
		on LocalEvent goto Available;
	}

	state Available
	{
		on NotifyLeaderUpdate do (payload : (machine, int))
		{
			UpdateLeader(payload.0, payload.1);
		}
		on ShutDown do
		{
			var idx : int;

			idx = 0;
			while (idx < NumberOfServers)
			{
				send Servers[idx], ShutDown;
				idx = idx + 1;
			}
			raise halt;
		}
		on LocalEvent goto Unavailable;
	}

	fun UpdateLeader(LeaderMachine : machine, Term : int)
	{
		if (LeaderTerm < Term)
        {
            Leader = LeaderMachine;
            LeaderTerm = Term;
        }
	}
}
