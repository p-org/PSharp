event Config : machine;
event Local;

machine Client1
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
			send server, Event1;
            raise Local;
		}
		on Local goto Handling;
	}
}

