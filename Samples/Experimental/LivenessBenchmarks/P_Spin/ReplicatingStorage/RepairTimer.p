event RepairTimer_ConfigureEvent : machine;
event RepairTimer_StartTimer;
event RepairTimer_CancelTimer;
event RepairTimer_Timeout;
event RepairTimer_TickEvent;

machine RepairTimer
{
	var Target : machine;

	start state Init
	{
		on RepairTimer_ConfigureEvent do (target : machine)
		{
			Target = target;
            raise RepairTimer_StartTimer;
		}
		on RepairTimer_StartTimer goto Active;
	}

	state Active
	{
		entry
		{
			send this, RepairTimer_TickEvent;
		}
		on RepairTimer_TickEvent do 
		{
			if ($)
            {
                send Target, RepairTimer_Timeout;
            }

            send this, RepairTimer_TickEvent;
		}
		on RepairTimer_CancelTimer goto Inactive;
		ignore RepairTimer_StartTimer;
	}

	state Inactive
	{
		on RepairTimer_StartTimer goto Active;
		ignore RepairTimer_CancelTimer, RepairTimer_TickEvent;
	}
}
