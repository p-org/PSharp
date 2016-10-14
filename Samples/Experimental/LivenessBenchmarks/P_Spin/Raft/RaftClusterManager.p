#include "Server.p"
#include "ElectionTimer.p"

event NotifyLeaderUpdate : (machine, int);
event ShutDown;
event LocalEvent;

machine Main
{
	var Servers : seq[machine];
	var NumberOfServers : int;
	var Leader : machine;
	var LeaderTerm : int;

	start state init
	{
		entry
		{
			var index : int;
			var serv : machine;

			NumberOfServers = 5;
            LeaderTerm = 0;
            Servers = default(seq[machine]);

			index = 0;
			while (index < NumberOfServers)
			{
				serv = new Server();
				Servers += (index, serv);
				index = index + 1;
			}    
            raise LocalEvent;
		}
		on LocalEvent goto Configuring;
	}
	
	state Configuring
	{
		entry
		{	
			var index : int;

			index = 0;
			while (index < NumberOfServers)
			{	
				send Servers[index], ConfigureEvent, index, Servers, this;				
				index = index + 1;
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
			var index : int;

			index = 0;
			while (index < NumberOfServers)
			{	
				send Servers[index], ShutDown;
				index = index + 1;
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
			var index : int;

			index = 0;
			while (index < NumberOfServers)
			{	
				send Servers[index], ShutDown;
				index = index + 1;
			}
            raise halt;
		}
		on LocalEvent goto Unavailable;
	}

	fun UpdateLeader(leader : machine, term : int)
    {
        if (LeaderTerm < term)
        {
            Leader = leader;
            LeaderTerm = term;
        }
    }
}