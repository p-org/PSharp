event PeriodicTimer_ConfigureEvent : machine;
event PeriodicTimer_StartTimer;
event PeriodicTimer_CancelTimer;
event PeriodicTimer_Timeout;
event PeriodicTimer_TickEvent;

machine PeriodicTimer
{
	var Target : machine;

	start state Init 
	{
		on PeriodicTimer_ConfigureEvent do (target : machine)
		{
			Target = target;
            //this.Raise(new StartTimer());
		}
		on PeriodicTimer_StartTimer goto Active;
	}

	state Active
	{
		entry
		{
			send this, PeriodicTimer_TickEvent;
		}
		on PeriodicTimer_TickEvent do
		{
			if ($)
            {
                send Target, PeriodicTimer_Timeout;
            }

            //this.Send(this.Id, new TickEvent());
            raise PeriodicTimer_CancelTimer;
		}
		on PeriodicTimer_CancelTimer goto Inactive;
		ignore PeriodicTimer_StartTimer;
	}
	state Inactive
	{
		on PeriodicTimer_StartTimer goto Active;
		ignore PeriodicTimer_CancelTimer, PeriodicTimer_TickEvent;
	}
}

