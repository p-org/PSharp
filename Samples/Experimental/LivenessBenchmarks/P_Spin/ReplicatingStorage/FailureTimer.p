event FailureTimer_ConfigureEvent : machine;
event FailureTimer_StartTimer;
event FailureTimer_CancelTimer;
event FailureTimer_Timeout;
event FailureTimer_TickEvent;

machine FailureTimer
{
	var Target : machine;

	start state Init
	{
		on FailureTimer_ConfigureEvent do (target : machine)
		{
			Target = target;
            raise FailureTimer_StartTimer;
		}
		on FailureTimer_StartTimer goto Active;
	}

	state Active
	{
		entry
		{
			send this, FailureTimer_TickEvent;
		}
		on FailureTimer_TickEvent do
		{
			if ($)
            {
                send Target, FailureTimer_Timeout;
            }

            send this, FailureTimer_TickEvent;
		}
		on FailureTimer_CancelTimer goto Inactive;
		ignore FailureTimer_StartTimer;
	}

	state Inactive
	{
		on FailureTimer_StartTimer goto Active;
		ignore FailureTimer_CancelTimer, FailureTimer_TickEvent;
	}
}
