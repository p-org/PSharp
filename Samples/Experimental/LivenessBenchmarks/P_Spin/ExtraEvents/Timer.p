event Unit;
event TimerTickEvent;

machine Timer
{
	var Target : machine;

	start state Init
	{
		entry (target : machine)
		{
			Target = target;
            
            raise Unit;
		}
		on Unit goto Active;
	}

	state Active
	{
		entry
		{
            if ($$)
            {
				send Target, TimerTickEvent;
            }
            
            raise Unit;
		}
		on Unit goto Active;
	}
}

