event ElectionTimer_ConfigureEvent : machine;
event ElectionTimer_StartTimer;
event ElectionTimer_CancelTimer;
event ElectionTimer_Timeout;
event ElectionTimer_TickEvent;

machine ElectionTimer
{
	var Target : machine;

	start state Init
	{
		on ElectionTimer_ConfigureEvent do (target : machine)
		{
			Target = target;
			//raise ElectionTimer_StartTimer;
		}
		on ElectionTimer_StartTimer goto Active;
	}

	state Active
	{	
		entry
		{
			send this, ElectionTimer_TickEvent;
		}
		on ElectionTimer_TickEvent do 
		{
			if ($)
            {
                send Target, ElectionTimer_Timeout;
            }

			//send this, ElectionTimer_TickEvent;
            raise ElectionTimer_CancelTimer;
		}
		on ElectionTimer_CancelTimer goto Inactive;
		ignore ElectionTimer_StartTimer;
	}

	state Inactive
	{
		on ElectionTimer_StartTimer goto Active;
		ignore ElectionTimer_CancelTimer, ElectionTimer_TickEvent;
	}
}