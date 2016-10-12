event SyncTimer_ConfigureEvent : machine;
event SyncTimer_StartTimer;
event SyncTimer_CancelTimer;
event SyncTimer_Timeout;
event SyncTimer_TickEvent;

machine SyncTimer
{
	var Target : machine;

	start state Init
	{
		on SyncTimer_ConfigureEvent do (target : machine)
		{
			Target = target;
            raise SyncTimer_StartTimer;
		}
		on SyncTimer_StartTimer goto Active;
	}

	state Active
	{
		entry
		{
			send this, SyncTimer_TickEvent;
		}
		on SyncTimer_TickEvent do 
		{
			if ($)
            {
                send Target, SyncTimer_Timeout;
            }

            send this, SyncTimer_TickEvent;
		}
		on SyncTimer_CancelTimer goto Inactive;
		ignore SyncTimer_StartTimer;
	}

	state Inactive
	{
		on SyncTimer_StartTimer goto Active;
		ignore SyncTimer_CancelTimer, SyncTimer_TickEvent;
	}
}
