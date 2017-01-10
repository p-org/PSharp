
machine Client2
{
	var server : machine;

	start state Init
	{
		entry (target : machine)
		{
            server = target;
            raise Local;
		}
		on Local goto Handling;
	}

	state Handling
	{
		entry
		{
			send server, Event2;
            raise Local;
		}
		on Local goto Handling;
	}
}

