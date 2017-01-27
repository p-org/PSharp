event ElectionTimer_ConfigureEvent : machine;
event ElectionTimer_StartTimer;
event ElectionTimer_CancelTimer;
event ElectionTimer_Timeout;
event ElectionTimer_TickEvent;
event Local;

machine ElectionTimer
{
	var Target : machine;

	start state Init
	{
		on ElectionTimer_ConfigureEvent do (target : machine)
		{
			Target = target;
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

			raise ElectionTimer_CancelTimer;
		}
		on ElectionTimer_CancelTimer goto Inactive;
		ignore ElectionTimer_StartTimer, Local;
	}

	state Inactive
	{
		entry
		{
			send this, Local;
		}
		on ElectionTimer_StartTimer goto Active;
		on Local goto Inactive;
		ignore ElectionTimer_CancelTimer, ElectionTimer_TickEvent;
	}
}