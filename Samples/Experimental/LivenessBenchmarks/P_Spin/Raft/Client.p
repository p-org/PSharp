event Client_ConfigureEvent : machine;
event Client_Request : (machine, int);
event Response;
event LocalEvent;

machine Client
{
	var Cluster : machine;
	var LatestCommand : int;
	var Counter : int;

	start state Init 
	{
		entry
		{
			LatestCommand = -1;
            Counter = 0;
		}
		on Client_ConfigureEvent do (cluster : machine)
		{
			Cluster = cluster;
            raise LocalEvent;
		}
		on LocalEvent goto PumpRequest;
	}

	state PumpRequest
	{
		entry
		{
			var index : int;
			var flag : bool;

			index = 0;
			flag = true;
			while(index < 100 && flag == true)
			{
				if($)
				{
					flag = false;
					LatestCommand = index;
				}
				index = index + 1;
			}
            Counter = Counter + 1;
            send Cluster, Client_Request, this, LatestCommand;
		}
		on Response do 
		{
			if (Counter == 3)
            {
                send Cluster, ShutDown;
                raise halt;
            }
            else
            {
                raise LocalEvent;
            }
		}
		on LocalEvent goto PumpRequest;
	}
}