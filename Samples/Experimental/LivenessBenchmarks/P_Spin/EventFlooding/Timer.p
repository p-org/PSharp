event Unit;
event TimerTickEvent;

machine Timer
{
	var Target : machine;
	var Counter : int;

	start state Init
	{
		entry (target : machine)
		{
			Target = target;
            Counter = 0;

            raise Unit;
		}
		on Unit goto Active;
	}

	state Active
	{
		entry
		{
			Counter = Counter + 1;
            if ($)
            {
				if(Counter == 10)
				{
					send Target, TimerTickEvent;
					Counter = 0;
				}
            }
            
            raise Unit;
		}
		on Unit goto Active;
	}
}

