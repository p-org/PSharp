event NotifyLeaderUpdate : (machine, int);
event RedirectRequest : event;
event ShutDown;
event Local;

machine ClusterManager
{
	var Servers : seq[machine];
	var NumberOfServers : int;
	var Leader : machine;
	var LeaderTerm : int;
	var Client : machine;

	start state Init 
	{
		entry
		{
			var index : int;

			NumberOfServers = 5;
			LeaderTerm = 0;
			Servers = default(seq[machine]);

			index = 0;
			while(index < NumberOfServers)
			{
				Servers[index] = new Server();
				index = index + 1;
			}
			Client = new Client();
			raise Local;
		}
		on Local goto Configuring;
	}

	state Configuring
	{
		entry
		{	
			var index : int;
			index = 0;
			while (index < NumberOfServers)
			{
				send Servers[index], Server_ConfigureEvent, index, Servers, this;
				index = index + 1;
			}
            send Client, Client_ConfigureEvent, this;

            raise Local;
		}
		on Local goto Unavailable
	}

	state Unavailable
	{
		on NotifyLeaderUpdate do (payload : event)
		{
			UpdateLeader(payload);
            raise Local;
		}
		on ShutDown do 
		{
			var index : int;
			index = 0;
			while(index < NumberOfServers)
			{
				send Servers[index], ShutDown;
				index = index + 1;
			}
            raise halt;
		}
		on Local goto Available;
		defer Client_Request;
	}

	state Available
	{
		on Client_Request do (payload : machine, int)
		{
			send Leader, Client_Request, paylaod.0, payload.1;
		}
		on RedirectRequest do (payload : event)
		{
			send this, payload;
		}
		on NotifyLeaderUpdate do (payload : event)
		{
			UpdateLeader(payload)
		}
		on ShutDown do 
		{
			var index : int;
			index = 0;
			while(index < NumberOfServers)
			{
				send Servers[index], ShutDown;
				index = index + 1;
			}
            raise halt;
		}
		on Local goto Unavailable;
	}

	fun UpdateLeader(var request : NotifyLeaderUpdate)
    {
        if (LeaderTerm < request.1)
        {
			Leader = request.0;
            LeaderTerm = request.1;
		}
	}
}
