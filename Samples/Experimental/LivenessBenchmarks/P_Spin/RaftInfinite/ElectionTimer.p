event ConfigureEvent : machine;
event StartTimer;
event CancelTimer;
event Timeout;
event TickEvent;

machine ElectionTimer
{
	var Target : machine;
	
	start state Init
	{
		on ConfigureEvent do (target : machine)
		{
			Target = target;
		}
		on StartTimer goto Active;
	}	

	state Active
	{
		entry
		{
			send this, TickEvent;
		}
		on TickEvent do
		{
			if ($)
            {
                send Target, Timeout;
            }
			raise CancelTimer;
		}
		on CancelTimer goto Inactive;
		ignore StartTimer;
	}

	state Inactive
	{
		entry
		{
			send this, StartTimer;
		}
		on StartTimer goto Active;
		ignore CancelTimer, TickEvent;
	}
}
