event Lk_Machine_AtomicTestSet : machine;
event Lk_Machine_AtomicTestSet_Resp;
event Lk_Machine_SetReq : (machine, bool);
event Lk_Machine_SetResp;
event Lk_Machine_Waiting : (machine, bool);
event Lk_Machine_WaitResp;

machine Lk_Machine
{
	var lk : bool;
	var blockedMachines : map[machine, bool];

	fun Unblock()
	{
		var remove : seq[machine];
		var index : int;

		index = 0;
		while(index < sizeof(blockedMachines))
		{
			if(values(blockedMachines)[index] == lk)
			{
				send keys(blockedMachines)[index], Lk_Machine_WaitResp;
				remove += (sizeof(remove), keys(blockedMachines)[index]);
			}
			index = index + 1;
		}

		index = 0;
		while (index < sizeof(remove))
		{
			blockedMachines -= remove[index];
			index = index + 1;
		}
	}

	start state Init 
	{
		entry
		{
			lk = false;
            blockedMachines = default(map[machine, bool]);
		}

		on Lk_Machine_AtomicTestSet do (target : machine)
		{
            if(lk == false)
            {
                lk = true;
                Unblock();                
            }
            send target, Lk_Machine_AtomicTestSet_Resp;
		}

		on Lk_Machine_SetReq do (payload : (machine, bool))
		{
            lk = payload.1;
            Unblock();
            send payload.0, Lk_Machine_SetResp;
		}

		on Lk_Machine_Waiting do (payload : (machine, bool))
		{
            if(lk == payload.1)
            {
                send payload.0, Lk_Machine_WaitResp;
            }
            else
            {
                blockedMachines += (payload.0, payload.1);
            }
		}
	}
}