event Client_ConfigureEvent : machine;
event Client_Request : (machine, int);

machine Client
{
	var NodeManager : machine;
	var Counter : int;

	start state Init
	{
		entry
		{
			Counter = 0;
		}
		on Client_ConfigureEvent do (nodeManager : machine)
		{
			NodeManager = nodeManager;
            raise LocalEvent;
		}
		on LocalEvent goto PumpRequest;
	}

	state PumpRequest
	{
		entry
		{
			var command : int;
			var index : int;
			var flag : bool;

			index = 0;
			flag = true;
			while (index <= 100 && flag == true)
			{
				if ($)
				{
					command = index;
					flag = false;
				}
				index = index + 1;
			}

			command = command + 1;
            Counter = Counter + 1;

            send NodeManager, Client_Request, this, command;

            if (Counter == 1)
            {
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