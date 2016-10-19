#include "Server.p"
#include "ElectionTimer.p"
#include "PeriodicTimer.p"
#include "Client.p"

event NotifyLeaderUpdate : (machine, int);
event ShutDown;
event LocalEvent;
event RedirectRequest : (machine, int);

machine Main
{
	var Servers : seq[machine];
	var NumberOfServers : int;
	var Leader : machine;
	var LeaderTerm : int;
	var Client : machine;

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
			Client = new Client();
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
			send Client, Client_ConfigureEvent, this;
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
		defer Request;
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
		on Request do (payload : (machine, int))
		{
			send Leader, Request, payload.0, payload.1;
		}
		on RedirectRequest do (payload : (machine, int))
		{
			send this, Request, payload.0, payload.1;
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