event State_Machine_ValueReq : machine;
event State_Machine_ValueResp : bool;
event State_Machine_SetReq : (machine, bool);
event State_Machine_SetResp;
event State_Machine_Waiting : (machine, bool);
event State_Machine_WaitResp;

machine State_Machine
{
	var State : bool;
	var blockedMachines : map[machine, bool];

	fun Unblock()
	{
		var remove : seq[machine];
		var index : int;
		var target : machine;

		index = 0;
		while(index < sizeof(blockedMachines))
		{
			target = keys(blockedMachines)[index];
			if(values(blockedMachines)[index] == State)
			{
				send target, State_Machine_WaitResp;
                remove += (sizeof(remove), target);
			}
			index = index + 1;
		}

		index = 0;
		while(index < sizeof(remove))
		{
			blockedMachines -= remove[index];
			index = index + 1;
		}
	}

	start state Init 
	{
		entry
		{
			State = false;
            blockedMachines = default(map[machine, bool]);
		}

		on State_Machine_SetReq do (payload : (machine, bool))
		{
            State = payload.1;
            Unblock();
            send payload.0, State_Machine_SetResp;
		}

		on State_Machine_ValueReq do (target : machine)
		{
            send target, State_Machine_ValueResp, State;
		}

		on State_Machine_Waiting do (payload : (machine, bool))
		{
            if (State == payload.1)
            {
                send payload.0, State_Machine_WaitResp;
            }
            else
            {
				blockedMachines += (payload.0, payload.1);
            }
		}
	}
}
        
     